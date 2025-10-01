import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Patient } from '../../models';

@Component({
  selector: 'app-patient-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './patient-management.component.html',
  styleUrls: ['./patient-management.component.css']
})
export class PatientManagementComponent implements OnInit {
  patients: Patient[] = [];
  patientForm: FormGroup;
  
  editingPatient: Patient | null = null;
  showForm = false;
  errorMessage = '';
  successMessage = '';
  loading = false;

  constructor(
    private apiService: ApiService,
    private fb: FormBuilder
  ) {
    this.patientForm = this.createPatientForm();
  }

  ngOnInit(): void {
    this.loadPatients();
  }

  private createPatientForm(): FormGroup {
    return this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^[\+]?[1-9][\d]{0,15}$/)]],
      dateOfBirth: ['', [Validators.required, this.dateOfBirthValidator]],
      address: ['', [Validators.maxLength(200)]],
      medicalHistory: ['', [Validators.maxLength(1000)]]
    });
  }

  private dateOfBirthValidator(control: any) {
    if (!control.value) return null;
    
    const birthDate = new Date(control.value);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    
    if (age < 0) {
      return { futureDate: true };
    }
    
    if (age > 120) {
      return { invalidAge: true };
    }
    
    return null;
  }

  loadPatients(): void {
    this.loading = true;
    this.apiService.getPatients().subscribe({
      next: (patients) => {
        this.patients = patients;
        this.loading = false;
        this.clearMessages();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load patients: ' + error.message;
        this.loading = false;
      }
    });
  }

  showAddForm(): void {
    this.editingPatient = null;
    this.showForm = true;
    this.patientForm.reset();
    this.clearMessages();
  }

  showEditForm(patient: Patient): void {
    this.editingPatient = { ...patient };
    this.showForm = true;
    this.patientForm.patchValue(patient);
    this.clearMessages();
  }

  savePatient(): void {
    if (this.patientForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    const patientData = this.patientForm.value;

    if (this.editingPatient) {
      this.updatePatient(patientData);
    } else {
      this.createPatient(patientData);
    }
  }

  createPatient(patientData: any): void {
    this.apiService.createPatient(patientData).subscribe({
      next: (patient) => {
        this.patients.push(patient);
        this.showForm = false;
        this.successMessage = 'Patient created successfully!';
        this.clearMessages();
      },
      error: (error) => {
        this.errorMessage = 'Failed to create patient: ' + error.message;
      }
    });
  }

  updatePatient(patientData: any): void {
    if (!this.editingPatient?.id) return;
    
    this.apiService.updatePatient(this.editingPatient.id, patientData).subscribe({
      next: (updatedPatient) => {
        const index = this.patients.findIndex(p => p.id === updatedPatient.id);
        if (index !== -1) {
          this.patients[index] = updatedPatient;
        }
        this.showForm = false;
        this.editingPatient = null;
        this.successMessage = 'Patient updated successfully!';
        this.clearMessages();
      },
      error: (error) => {
        this.errorMessage = 'Failed to update patient: ' + error.message;
      }
    });
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingPatient = null;
    this.patientForm.reset();
    this.clearMessages();
  }

  private markFormGroupTouched(): void {
    Object.keys(this.patientForm.controls).forEach(key => {
      const control = this.patientForm.get(key);
      control?.markAsTouched();
    });
  }

  // Form validation getters
  get firstName() { return this.patientForm.get('firstName'); }
  get lastName() { return this.patientForm.get('lastName'); }
  get email() { return this.patientForm.get('email'); }
  get phoneNumber() { return this.patientForm.get('phoneNumber'); }
  get dateOfBirth() { return this.patientForm.get('dateOfBirth'); }
  get address() { return this.patientForm.get('address'); }
  get medicalHistory() { return this.patientForm.get('medicalHistory'); }

  deletePatient(patient: Patient): void {
    if (!patient.id) return;
    
    if (confirm(`Are you sure you want to delete ${patient.firstName} ${patient.lastName}?`)) {
      this.apiService.deletePatient(patient.id).subscribe({
        next: () => {
          this.patients = this.patients.filter(p => p.id !== patient.id);
          this.successMessage = 'Patient deleted successfully!';
        },
        error: (error) => {
          this.errorMessage = 'Failed to delete patient: ' + error.message;
        }
      });
    }
  }


  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }
}
