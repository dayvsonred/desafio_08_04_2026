import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ComplaintComponent } from './pages/complaint/complaint.component';
import { MetricsComponent } from './pages/metrics/metrics.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'metrics' },
  { path: 'metrics', component: MetricsComponent },
  { path: 'reclamacoes', component: ComplaintComponent },
  { path: '**', redirectTo: 'metrics' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
