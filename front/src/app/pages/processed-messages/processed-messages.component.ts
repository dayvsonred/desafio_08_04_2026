import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { GatewayService, ProcessedMessageItem } from '../../core/services/gateway.service';

type SearchBy = 'complaintId' | 'correlationId';

@Component({
  selector: 'app-processed-messages',
  templateUrl: './processed-messages.component.html',
  styleUrls: ['./processed-messages.component.scss']
})
export class ProcessedMessagesComponent {
  readonly searchForm = this.formBuilder.group({
    searchBy: this.formBuilder.nonNullable.control<SearchBy>('correlationId'),
    value: this.formBuilder.nonNullable.control('', [Validators.required, Validators.minLength(8)]),
    limit: this.formBuilder.nonNullable.control(100, [Validators.required, Validators.min(1), Validators.max(500)])
  });

  readonly displayedColumns: string[] = ['complaintId', 'correlationId', 'day', 'processedAtUtc'];

  isLoading = false;
  errorMessage = '';
  total = 0;
  searchByLabel = '';
  searchValue = '';
  items: ProcessedMessageItem[] = [];

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly gatewayService: GatewayService
  ) { }

  get selectedSearchBy(): SearchBy {
    return this.searchForm.controls.searchBy.value;
  }

  get searchFieldLabel(): string {
    return this.selectedSearchBy === 'complaintId' ? 'Complaint ID' : 'Correlation ID';
  }

  get searchPlaceholder(): string {
    return this.selectedSearchBy === 'complaintId'
      ? 'Ex: f6037f5d3a7b49f49f989382717a2f6e'
      : 'Ex: 0f3b0280-8dbd-43c7-8762-74d968ea88d8';
  }

  setSearchBy(searchBy: SearchBy): void {
    this.searchForm.controls.searchBy.setValue(searchBy);
    this.searchForm.controls.value.reset('');
    this.items = [];
    this.errorMessage = '';
    this.total = 0;
    this.searchByLabel = '';
    this.searchValue = '';
  }

  searchProcessedMessages(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    const searchBy = this.searchForm.controls.searchBy.value;
    const value = this.searchForm.controls.value.value.trim();
    const limit = this.searchForm.controls.limit.value;

    this.isLoading = true;
    this.errorMessage = '';
    this.items = [];
    this.total = 0;

    const request$ = searchBy === 'complaintId'
      ? this.gatewayService.getProcessedMessagesByComplaintId(value, limit)
      : this.gatewayService.getProcessedMessagesByCorrelationId(value, limit);

    request$
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe({
        next: (response) => {
          this.searchByLabel = searchBy === 'complaintId' ? 'Complaint ID' : 'Correlation ID';
          this.searchValue = value;
          this.total = response.total;
          this.items = response.items;
        },
        error: (error) => {
          const backendMessage = error?.error?.error;
          this.errorMessage = backendMessage || 'Nao foi possivel buscar mensagens processadas para o filtro informado.';
        }
      });
  }
}
