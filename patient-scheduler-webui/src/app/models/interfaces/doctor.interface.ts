export interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  specialization: string;
  email: string;
  phoneNumber: string;
  officeAddress?: string;
  hourlyRate: number;
}

export interface CreateDoctorRequest {
  firstName: string;
  lastName: string;
  specialization: string;
  email: string;
  phoneNumber: string;
  officeAddress?: string;
  hourlyRate: number;
}

export interface UpdateDoctorRequest extends Partial<CreateDoctorRequest> {
  id: number;
}
