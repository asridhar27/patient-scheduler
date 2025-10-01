export interface TimeSlot {
  id: number;
  startTime: Date;
  endTime: Date;
  isAvailable: boolean;
}

export interface CreateTimeSlotRequest {
  startTime: Date;
  endTime: Date;
  doctorId: number;
}

export interface UpdateTimeSlotRequest extends Partial<CreateTimeSlotRequest> {
  id: number;
}
