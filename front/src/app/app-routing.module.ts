import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ComplaintComponent } from './pages/complaint/complaint.component';
import { MetricsComponent } from './pages/metrics/metrics.component';
import { ProcessedMessagesComponent } from './pages/processed-messages/processed-messages.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'metrics' },
  { path: 'metrics', component: MetricsComponent },
  { path: 'processadas', component: ProcessedMessagesComponent },
  { path: 'reclamacoes', component: ComplaintComponent },
  { path: '**', redirectTo: 'metrics' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
