import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Invoice, Appointment } from '../../models';
import { PdfService, InvoiceData } from '../../services/pdf.service';

@Component({
  selector: 'app-payment-invoice',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-invoice.component.html',
  styleUrls: ['./payment-invoice.component.css']
})
export class PaymentInvoiceComponent implements OnInit {
  invoices: Invoice[] = [];
  filteredInvoices: Invoice[] = [];
  selectedInvoice: Invoice | null = null;
  
  // Filter options
  statusFilter = 'all';
  patientFilter = '';
  
  errorMessage = '';
  successMessage = '';
  showPaymentForm = false;
  loading = false;
  
  // Payment form
  paymentAmount = 0;
  paymentMethod = 'credit_card';
  paymentNotes = '';

  constructor(private apiService: ApiService, private pdfService: PdfService) { }

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.loading = true;
    this.apiService.getInvoices().subscribe({
      next: (invoices) => {
        this.invoices = invoices;
        this.applyFilters();
        this.loading = false;
        this.clearMessages();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load invoices: ' + error.message;
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.filteredInvoices = this.invoices.filter(invoice => {
      const matchesStatus = this.statusFilter === 'all' || invoice.status.toLowerCase() === this.statusFilter.toLowerCase();
      const matchesPatient = !this.patientFilter || 
        (invoice.patientName && invoice.patientName.toLowerCase().includes(this.patientFilter.toLowerCase()));
      
      return matchesStatus && matchesPatient;
    });
  }

  onStatusFilterChange(): void {
    this.applyFilters();
  }

  onPatientFilterChange(): void {
    this.applyFilters();
  }

  showPaymentModal(invoice: Invoice): void {
    this.selectedInvoice = invoice;
    this.paymentAmount = invoice.amount;
    this.paymentMethod = 'credit_card';
    this.paymentNotes = '';
    this.showPaymentForm = true;
    this.clearMessages();
  }

  processPayment(): void {
    if (!this.selectedInvoice?.id) return;

    this.apiService.processPayment(this.selectedInvoice.id).subscribe({
      next: () => {
        this.loadInvoices(); // Reload to get updated status
        this.showPaymentForm = false;
        this.selectedInvoice = null;
        this.successMessage = 'Payment processed successfully!';
        this.clearMessages();
      },
      error: (error) => {
        this.errorMessage = 'Failed to process payment: ' + error.message;
      }
    });
  }

  cancelPayment(): void {
    this.showPaymentForm = false;
    this.selectedInvoice = null;
    this.clearMessages();
  }

  downloadInvoice(invoice: Invoice): void {
    // Generate and download PDF invoice
    const invoiceData: InvoiceData = {
      id: invoice.id!,
      amount: invoice.amount,
      status: invoice.status,
      dueDate: invoice.dueDate,
      paidDate: invoice.paidDate,
      notes: invoice.notes,
      createdAt: new Date().toISOString(), // Use current date as fallback
      patientName: invoice.patientName || 'Unknown Patient',
      doctorName: invoice.doctorName || 'Unknown Doctor',
      appointmentDateTime: invoice.appointmentDateTime || new Date().toISOString(),
    };
    
    this.pdfService.generateInvoicePdf(invoiceData);
    this.successMessage = 'Invoice PDF downloaded successfully!';
    this.clearMessages();
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'pending': return 'status-pending';
      case 'paid': return 'status-paid';
      case 'overdue': return 'status-overdue';
      case 'cancelled': return 'status-cancelled';
      case 'refunded': return 'status-refunded';
      default: return 'status-unknown';
    }
  }

  getStatusIcon(status: string): string {
    switch (status?.toLowerCase()) {
      case 'pending': return '⏳';
      case 'paid': return '✅';
      case 'overdue': return '⚠️';
      case 'cancelled': return '❌';
      case 'refunded': return '🔄';
      default: return '❓';
    }
  }

  isOverdue(invoice: Invoice): boolean {
    if (invoice.status.toLowerCase() === 'paid') return false;
    return new Date(invoice.dueDate) < new Date();
  }

  private clearMessages(): void {
    setTimeout(() => {
      this.errorMessage = '';
      this.successMessage = '';
    }, 5000);
  }
}
