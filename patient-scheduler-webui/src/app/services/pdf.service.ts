import { Injectable } from '@angular/core';
import jsPDF from 'jspdf';

export interface InvoiceData {
  id: number;
  amount: number;
  status: string;
  dueDate: string;
  paidDate?: string;
  notes?: string;
  createdAt: string;
  patientName: string;
  doctorName: string;
  appointmentDateTime: string;
  patientEmail?: string;
  patientPhone?: string;
  patientAddress?: string;
  doctorSpecialization?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PdfService {

  generateInvoicePdf(invoiceData: InvoiceData): void {
    const doc = new jsPDF();
    
    // Set up colors
    const primaryColor = [41, 128, 185]; // Blue
    const secondaryColor = [52, 73, 94]; // Dark gray
    const lightGray = [236, 240, 241]; // Light gray
    
    // Title
    doc.setFontSize(24);
    doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
    doc.text('INVOICE', 105, 30, { align: 'center' });
    
    // Invoice details
    doc.setFontSize(10);
    doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
    doc.text(`Invoice #: INV-${invoiceData.id.toString().padStart(6, '0')}`, 20, 50);
    doc.text(`Date: ${new Date(invoiceData.createdAt).toLocaleDateString()}`, 20, 60);
    doc.text(`Due Date: ${new Date(invoiceData.dueDate).toLocaleDateString()}`, 20, 70);
    doc.text(`Status: ${invoiceData.status}`, 20, 80);
    
    // Bill to section
    doc.setFontSize(12);
    doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
    doc.text('BILL TO:', 20, 100);
    
    doc.setFontSize(10);
    doc.setTextColor(0, 0, 0);
    doc.text(invoiceData.patientName, 20, 110);
    if (invoiceData.patientEmail) {
      doc.text(invoiceData.patientEmail, 20, 120);
    }
    if (invoiceData.patientPhone) {
      doc.text(invoiceData.patientPhone, 20, 130);
    }
    if (invoiceData.patientAddress) {
      doc.text(invoiceData.patientAddress, 20, 140);
    }
    
    // Service details
    doc.setFontSize(12);
    doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
    doc.text('SERVICE DETAILS:', 20, 160);
    
    doc.setFontSize(10);
    doc.setTextColor(0, 0, 0);
    doc.text(`Doctor: ${invoiceData.doctorName}`, 20, 170);
    if (invoiceData.doctorSpecialization) {
      doc.text(`Specialization: ${invoiceData.doctorSpecialization}`, 20, 180);
    }
    doc.text(`Appointment Date: ${new Date(invoiceData.appointmentDateTime).toLocaleString()}`, 20, 190);
    
    // Amount table
    const tableTop = 210;
    const tableWidth = 100;
    const cellHeight = 10;
    
    // Table header
    doc.setFillColor(lightGray[0], lightGray[1], lightGray[2]);
    doc.rect(120, tableTop, tableWidth, cellHeight, 'F');
    doc.setTextColor(0, 0, 0);
    doc.setFontSize(10);
    doc.text('Description', 125, tableTop + 7);
    doc.text('Amount', 200, tableTop + 7);
    
    // Service row
    doc.rect(120, tableTop + cellHeight, tableWidth, cellHeight);
    doc.text('Medical Consultation', 125, tableTop + cellHeight + 7);
    doc.text(`$${invoiceData.amount.toFixed(2)}`, 200, tableTop + cellHeight + 7);
    
    // Total row
    doc.setFillColor(lightGray[0], lightGray[1], lightGray[2]);
    doc.rect(120, tableTop + (cellHeight * 2), tableWidth, cellHeight, 'F');
    doc.setFontSize(12);
    doc.text('TOTAL', 125, tableTop + (cellHeight * 2) + 7);
    doc.text(`$${invoiceData.amount.toFixed(2)}`, 200, tableTop + (cellHeight * 2) + 7);
    
    // Notes section
    if (invoiceData.notes) {
      doc.setFontSize(10);
      doc.setTextColor(0, 0, 0);
      doc.text('Notes:', 20, tableTop + (cellHeight * 4) + 10);
      doc.text(invoiceData.notes, 20, tableTop + (cellHeight * 4) + 20);
    }
    
    // Footer
    doc.setFontSize(10);
    doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
    doc.text('Thank you for choosing our medical services.', 105, 280, { align: 'center' });
    
    // Download the PDF
    const fileName = `Invoice-${invoiceData.id}-${new Date().toISOString().split('T')[0]}.pdf`;
    doc.save(fileName);
  }
}
