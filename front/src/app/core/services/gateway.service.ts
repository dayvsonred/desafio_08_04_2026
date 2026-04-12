import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DailyMetricsResponse {
  day: string;
  receivedCount: number;
  classifiedCount: number;
  classificationFailedCount: number;
  processedSuccessCount: number;
  processedErrorCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface MetricMessageEventItem {
  complaintId: string;
  correlationId: string;
  eventType: string;
  eventCreatedAtUtc: string;
}

export interface MetricMessageEventsResponse {
  day: string;
  eventType: string;
  total: number;
  items: MetricMessageEventItem[];
}

export interface ProcessedMessageItem {
  complaintId: string;
  correlationId: string;
  day: string;
  processedAtUtc: string;
}

export interface ProcessedMessagesResponse {
  searchBy: 'complaintId' | 'correlationId';
  searchValue: string;
  total: number;
  items: ProcessedMessageItem[];
}

export interface ComplaintRequest {
  reclamacao: string;
}

export interface ComplaintResponse {
  complaintId: string;
  correlationId: string;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class GatewayService {
  private readonly baseUrl = environment.gatewayApiBaseUrl.replace(/\/+$/, '');

  constructor(private readonly httpClient: HttpClient) { }

  getDailyMetrics(day: string): Observable<DailyMetricsResponse> {
    const params = new HttpParams().set('day', day);
    return this.httpClient.get<DailyMetricsResponse>(`${this.baseUrl}/metrics`, { params });
  }

  getMetricMessageEvents(
    day: string,
    eventType: 'RECEIVED' | 'PROCESSED',
    limit = 100
  ): Observable<MetricMessageEventsResponse> {
    const params = new HttpParams()
      .set('day', day)
      .set('eventType', eventType)
      .set('limit', `${limit}`);

    return this.httpClient.get<MetricMessageEventsResponse>(`${this.baseUrl}/metrics/events`, { params });
  }

  getProcessedMessagesByComplaintId(complaintId: string, limit = 100): Observable<ProcessedMessagesResponse> {
    const params = new HttpParams()
      .set('complaintId', complaintId)
      .set('limit', `${limit}`);

    return this.httpClient.get<ProcessedMessagesResponse>(`${this.baseUrl}/metrics/processed`, { params });
  }

  getProcessedMessagesByCorrelationId(correlationId: string, limit = 100): Observable<ProcessedMessagesResponse> {
    const params = new HttpParams()
      .set('correlationId', correlationId)
      .set('limit', `${limit}`);

    return this.httpClient.get<ProcessedMessagesResponse>(`${this.baseUrl}/metrics/processed`, { params });
  }

  sendComplaint(payload: ComplaintRequest, correlationId?: string): Observable<ComplaintResponse> {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });

    if (correlationId) {
      headers = headers.set('x-correlation-id', correlationId);
    }

    return this.httpClient.post<ComplaintResponse>(`${this.baseUrl}/complaints`, payload, { headers });
  }
}
