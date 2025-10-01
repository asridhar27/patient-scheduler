import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  Patient,
  Appointment,
  Doctor,
  Invoice,
  TimeSlot,
  BulkOperationRequest,
  BulkOperationResult,
  JobStatus
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:8080/api';

  constructor(private http: HttpClient) { }

  // Patient operations
  getPatients(): Observable<Patient[]> {
    return this.http.get<Patient[]>(`${this.baseUrl}/patients`)
      .pipe(catchError(this.handleError));
  }

  createPatient(patient: Patient): Observable<Patient> {
    return this.http.post<Patient>(`${this.baseUrl}/patients`, patient)
      .pipe(catchError(this.handleError));
  }

  updatePatient(id: number, patient: Patient): Observable<Patient> {
    return this.http.put<Patient>(`${this.baseUrl}/patients/${id}`, patient)
      .pipe(catchError(this.handleError));
  }

  deletePatient(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/patients/${id}`)
      .pipe(catchError(this.handleError));
  }

  // Doctor operations
  getDoctors(): Observable<Doctor[]> {
    return this.http.get<Doctor[]>(`${this.baseUrl}/doctors`)
      .pipe(catchError(this.handleError));
  }

  // Appointment operations
  getAppointments(): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.baseUrl}/appointments`)
      .pipe(catchError(this.handleError));
  }

  createAppointment(appointment: any): Observable<Appointment> {
    return this.http.post<Appointment>(`${this.baseUrl}/appointments`, appointment)
      .pipe(catchError(this.handleError));
  }

  scheduleAppointmentWithBilling(appointment: any, amount: number, dueDate: string): Observable<any> {
    const params = new URLSearchParams();
    params.append('amount', amount.toString());
    params.append('dueDate', dueDate);
    
    return this.http.post<any>(`${this.baseUrl}/appointments/schedule-with-billing?${params.toString()}`, appointment)
      .pipe(catchError(this.handleError));
  }

  cancelAppointment(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/appointments/${id}/cancel`, {})
      .pipe(catchError(this.handleError));
  }

  cancelAppointmentWithRefund(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/appointments/${id}/cancel-with-refund`, {})
      .pipe(catchError(this.handleError));
  }

  // Time slot operations
  getAvailableTimeSlots(doctorId: number, date: string): Observable<TimeSlot[]> {
    return this.http.get<TimeSlot[]>(`${this.baseUrl}/timeslots/available?doctorId=${doctorId}&date=${date}`)
      .pipe(catchError(this.handleError));
  }

  // Bulk Operations
  queueBulkOperation(request: BulkOperationRequest): Observable<BulkOperationResult> {
    return this.http.post<BulkOperationResult>(`${this.baseUrl}/bulkoperations/queue`, request)
      .pipe(catchError(this.handleError));
  }

  getJobStatus(jobId: number): Observable<JobStatus> {
    return this.http.get<JobStatus>(`${this.baseUrl}/bulkoperations/job/${jobId}`)
      .pipe(catchError(this.handleError));
  }

  getAllJobs(): Observable<JobStatus[]> {
    return this.http.get<JobStatus[]>(`${this.baseUrl}/bulkoperations/jobs`)
      .pipe(catchError(this.handleError));
  }

  cancelJob(jobId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/bulkoperations/job/${jobId}/cancel`, {})
      .pipe(catchError(this.handleError));
  }

  getBulkOperationStats(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/bulkoperations/stats`)
      .pipe(catchError(this.handleError));
  }

  // Invoice operations
  getInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.baseUrl}/invoices`)
      .pipe(catchError(this.handleError));
  }

  processPayment(invoiceId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/invoices/${invoiceId}/process-payment`, {})
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      if (error.error && error.error.message) {
        errorMessage = error.error.message;
      }
    }
    
    return throwError(() => new Error(errorMessage));
  }
}