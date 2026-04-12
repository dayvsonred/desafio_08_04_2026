import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormControl, ValidationErrors, Validators } from '@angular/forms';
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
  readonly dayControl = new FormControl<Date | null>(null, [
    Validators.required,
    MetricsComponent.validDateValidator
  ]);

  readonly displayedColumns: string[] = ['label', 'value'];
  readonly drilldownDisplayedColumns: string[] = ['complaintId', 'correlationId', 'eventCreatedAtUtc'];

  isLoading = false;
  errorMessage = '';
  noDataForDay = '';
  metrics: DailyMetricsResponse | null = null;
  metricItems: MetricItem[] = [];
  selectedDrilldownLabel = '';
  selectedDrilldownType: DrilldownEventType | null = null;
  drilldownItems: MetricMessageEventItem[] = [];
  isDrilldownLoading = false;
  drilldownErrorMessage = '';

  constructor(private readonly gatewayService: GatewayService) { }

  ngOnInit(): void {
    this.dayControl.setValue(new Date());
    this.loadMetrics();
  }

  loadMetrics(): void {
    if (this.dayControl.invalid) {
      this.dayControl.markAsTouched();
      this.metrics = null;
      this.metricItems = [];
      this.noDataForDay = '';
      this.clearDrilldown();
      this.errorMessage = 'Selecione uma data valida para consulta.';
      return;
    }

    const day = this.getSelectedDayAsYyyyMmDd();
    if (!day) {
      this.dayControl.markAsTouched();
      this.metrics = null;
      this.metricItems = [];
      this.noDataForDay = '';
      this.clearDrilldown();
      this.errorMessage = 'Selecione uma data valida para consulta.';
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    this.noDataForDay = '';

    this.gatewayService.getDailyMetrics(day)
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe({
        next: (metrics) => {
          this.metrics = metrics;
          this.noDataForDay = this.isMetricsEmpty(metrics) ? metrics.day : '';
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
          this.noDataForDay = '';
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

    const day = this.getSelectedDayAsYyyyMmDd();
    if (!day) {
      return;
    }
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

  private isMetricsEmpty(metrics: DailyMetricsResponse): boolean {
    const total =
      metrics.receivedCount +
      metrics.classifiedCount +
      metrics.classificationFailedCount +
      metrics.processedSuccessCount +
      metrics.processedErrorCount;

    return total === 0;
  }

  private getSelectedDayAsYyyyMmDd(): string | null {
    const selectedDate = this.dayControl.value;
    if (!(selectedDate instanceof Date) || Number.isNaN(selectedDate.getTime())) {
      return null;
    }

    return this.formatAsYyyyMmDd(selectedDate);
  }

  private static validDateValidator(control: AbstractControl<Date | null>): ValidationErrors | null {
    const value = control.value;
    if (value === null) {
      return null;
    }

    return value instanceof Date && !Number.isNaN(value.getTime())
      ? null
      : { invalidDate: true };
  }

  private formatAsYyyyMmDd(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}${month}${day}`;
  }
}
