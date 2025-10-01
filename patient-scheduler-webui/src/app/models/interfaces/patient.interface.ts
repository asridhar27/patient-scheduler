export interface Patient {
  id?: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  address?: string;
  medicalHistory?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  address?: string;
  medicalHistory?: string;
}

export interface UpdatePatientRequest extends Partial<CreatePatientRequest> {
  id: number;
}
