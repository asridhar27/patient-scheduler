export interface BulkOperationRequest {
  operationType: string;
  appointmentIds: number[];
  reason?: string;
  newDateTime?: Date;
  newStatus?: string;
  billingAdjustment?: number;
  notes?: string;
}

export interface BulkOperationResult {
  jobId: number;
  status: string;
  totalRecords: number;
  processedRecords: number;
  failedRecords: number;
  errors: string[];
  createdAt: Date;
  completedAt?: Date;
}

export interface JobStatus {
  id: number;
  operationType: string;
  status: string;
  totalRecords: number;
  processedRecords: number;
  failedRecords: number;
  errors: string[];
  createdAt: Date;
  startedAt?: Date;
  completedAt?: Date;
  result?: string;
}
