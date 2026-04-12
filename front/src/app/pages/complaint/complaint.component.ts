import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { ComplaintResponse, GatewayService } from '../../core/services/gateway.service';

@Component({
  selector: 'app-complaint',
  templateUrl: './complaint.component.html',
  styleUrls: ['./complaint.component.scss']
})
export class ComplaintComponent implements OnInit {
  readonly form = this.formBuilder.group({
    reclamacao: ['', [Validators.required, Validators.minLength(10)]]
  });

  isSubmitting = false;
  errorMessage = '';
  result: ComplaintResponse | null = null;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly gatewayService: GatewayService,
    private readonly snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
  }

  submitComplaint(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.result = null;

    const reclamacao = this.form.controls.reclamacao.value ?? '';
    const correlationId = this.generateCorrelationId();

    this.gatewayService.sendComplaint({ reclamacao }, correlationId)
      .pipe(finalize(() => { this.isSubmitting = false; }))
      .subscribe({
        next: (response) => {
          this.result = response;
          this.snackBar.open('Reclamacao enviada com sucesso.', 'Fechar', {
            duration: 3000
          });
        },
        error: (error) => {
          const backendMessage = error?.error?.error;
          this.errorMessage = backendMessage || 'Falha ao enviar a reclamacao.';
        }
      });
  }

  private generateCorrelationId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }

    const timestamp = Date.now();
    const random = Math.random().toString(16).slice(2);
    return `web-${timestamp}-${random}`;
  }
}
