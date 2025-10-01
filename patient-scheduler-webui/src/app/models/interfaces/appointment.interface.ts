export interface Appointment {
  id?: number;
  patientId: number;
  doctorId: number;
  appointmentDateTime: string;
  duration: string;
  notes?: string;
  status?: string;
  patientName?: string;
  doctorName?: string;
  doctorSpecialization?: string;
  selectedDateTime?: Date;
}

export interface CreateAppointmentRequest {
  patientId: number;
  doctorId: number;
  appointmentDateTime: string;
  notes?: string;
  amount: number;
  dueDate: string;
}

export interface UpdateAppointmentRequest extends Partial<CreateAppointmentRequest> {
  id: number;
}

export interface AppointmentResponseDto {
  id: number;
  patientId: number;
  doctorId: number;
  appointmentDateTime: string;
  duration: string;
  notes?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  patientName: string;
  doctorName: string;
  doctorSpecialization: string;
}
