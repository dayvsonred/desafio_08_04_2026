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

  sendComplaint(payload: ComplaintRequest, correlationId?: string): Observable<ComplaintResponse> {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });

    if (correlationId) {
      headers = headers.set('x-correlation-id', correlationId);
    }

    return this.httpClient.post<ComplaintResponse>(`${this.baseUrl}/complaints`, payload, { headers });
  }
}
