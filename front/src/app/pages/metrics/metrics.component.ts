import { Component, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  DailyMetricsResponse,
  GatewayService,
  MetricMessageEventItem
} from '../../core/services/gateway.service';

type DrilldownEventType = 'RECEIVED' | 'PROCESSED';

interface MetricItem {
  label: string;
  value: number;
  drilldownEventType: DrilldownEventType | null;
}

@Component({
  selector: 'app-metrics',
  templateUrl: './metrics.component.html',
  styleUrls: ['./metrics.component.scss']
})
export class MetricsComponent implements OnInit {
  readonly dayControl = new FormControl('', [
    Validators.required,
    Validators.pattern(/^\d{8}$/)
  ]);

  readonly displayedColumns: string[] = ['label', 'value'];
  readonly drilldownDisplayedColumns: string[] = ['complaintId', 'correlationId', 'eventCreatedAtUtc'];

  isLoading = false;
  errorMessage = '';
  metrics: DailyMetricsResponse | null = null;
  metricItems: MetricItem[] = [];
  selectedDrilldownLabel = '';
  selectedDrilldownType: DrilldownEventType | null = null;
  drilldownItems: MetricMessageEventItem[] = [];
  isDrilldownLoading = false;
  drilldownErrorMessage = '';

  constructor(private readonly gatewayService: GatewayService) { }

  ngOnInit(): void {
    this.dayControl.setValue(this.formatAsYyyyMmDd(new Date()));
    this.loadMetrics();
  }

  loadMetrics(): void {
    if (this.dayControl.invalid) {
      this.dayControl.markAsTouched();
      return;
    }

    const day = this.dayControl.value ?? '';
    this.isLoading = true;
    this.errorMessage = '';

    this.gatewayService.getDailyMetrics(day)
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe({
        next: (metrics) => {
          this.metrics = metrics;
          this.metricItems = [
            { label: 'Recebidas', value: metrics.receivedCount, drilldownEventType: 'RECEIVED' },
            { label: 'Classificadas', value: metrics.classifiedCount, drilldownEventType: null },
            { label: 'Falha de classificacao', value: metrics.classificationFailedCount, drilldownEventType: null },
            { label: 'Processadas com sucesso', value: metrics.processedSuccessCount, drilldownEventType: 'PROCESSED' },
            { label: 'Processadas com erro', value: metrics.processedErrorCount, drilldownEventType: null }
          ];
          this.clearDrilldown();
        },
        error: (error) => {
          const backendMessage = error?.error?.error;
          this.metrics = null;
          this.metricItems = [];
          this.clearDrilldown();
          this.errorMessage = backendMessage || 'Nao foi possivel carregar as metricas para o dia informado.';
        }
      });
  }

  onMetricCardClick(item: MetricItem): void {
    if (!item.drilldownEventType || !this.metrics) {
      return;
    }

    this.loadMetricEvents(item.label, item.drilldownEventType);
  }

  private loadMetricEvents(label: string, eventType: DrilldownEventType): void {
    if (this.dayControl.invalid) {
      return;
    }

    const day = this.dayControl.value ?? '';
    this.selectedDrilldownLabel = label;
    this.selectedDrilldownType = eventType;
    this.drilldownItems = [];
    this.drilldownErrorMessage = '';
    this.isDrilldownLoading = true;

    this.gatewayService.getMetricMessageEvents(day, eventType)
      .pipe(finalize(() => { this.isDrilldownLoading = false; }))
      .subscribe({
        next: (response) => {
          this.drilldownItems = response.items;
        },
        error: (error) => {
          const backendMessage = error?.error?.error;
          this.drilldownErrorMessage = backendMessage || 'Nao foi possivel carregar os IDs desta metrica.';
        }
      });
  }

  private clearDrilldown(): void {
    this.selectedDrilldownLabel = '';
    this.selectedDrilldownType = null;
    this.drilldownItems = [];
    this.isDrilldownLoading = false;
    this.drilldownErrorMessage = '';
  }

  private formatAsYyyyMmDd(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}${month}${day}`;
  }
}
