// Common API response interface
export interface ApiResponse<T> {
  data?: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

// Utility interfaces
export type SortDirection = 'asc' | 'desc';

export interface SortConfig {
  field: string;
  direction: SortDirection;
}

export interface PaginationConfig {
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface FilterConfig {
  searchTerm?: string;
  status?: string;
  dateFrom?: Date;
  dateTo?: Date;
}
