export interface Invoice {
  id?: number;
  patientId: number;
  appointmentId: number;
  amount: number;
  status: string;
  dueDate: string;
  paidDate?: string;
  patientName?: string;
  doctorName?: string;
  appointmentDateTime?: string;
  notes?: string;
}

export interface CreateInvoiceRequest {
  patientId: number;
  appointmentId: number;
  amount: number;
  dueDate: string;
  notes?: string;
}

export interface UpdateInvoiceRequest extends Partial<CreateInvoiceRequest> {
  id: number;
}
