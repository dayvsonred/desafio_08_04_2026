import { Component, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { DailyMetricsResponse, GatewayService } from '../../core/services/gateway.service';

interface MetricItem {
  label: string;
  value: number;
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

  isLoading = false;
  errorMessage = '';
  metrics: DailyMetricsResponse | null = null;
  metricItems: MetricItem[] = [];

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
            { label: 'Recebidas', value: metrics.receivedCount },
            { label: 'Classificadas', value: metrics.classifiedCount },
            { label: 'Falha de classificacao', value: metrics.classificationFailedCount },
            { label: 'Processadas com sucesso', value: metrics.processedSuccessCount },
            { label: 'Processadas com erro', value: metrics.processedErrorCount }
          ];
        },
        error: (error) => {
          const backendMessage = error?.error?.error;
          this.metrics = null;
          this.metricItems = [];
          this.errorMessage = backendMessage || 'Nao foi possivel carregar as metricas para o dia informado.';
        }
      });
  }

  private formatAsYyyyMmDd(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}${month}${day}`;
  }
}
