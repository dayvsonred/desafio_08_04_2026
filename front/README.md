# Frontend Angular 14

Aplicacao Angular 14 com Angular Material para:

- consultar metricas diarias (`/metrics`)
- enviar reclamacoes para o API Gateway (`/reclamacoes`)

## Rotas

- `/metrics`: consulta `GET /metrics?day=yyyyMMdd` e exibe cards + tabela.
- `/reclamacoes`: envia `POST /complaints` com corpo `{ "reclamacao": "..." }`.

## Configuracao do endpoint

A URL base do API Gateway ja esta configurada em:

- `src/environments/environment.ts`
- `src/environments/environment.prod.ts`

Campo:

```ts
gatewayApiBaseUrl: 'https://3wzhy48jd6.execute-api.sa-east-1.amazonaws.com'
```

Se o ID do gateway mudar, atualize esse valor nos dois arquivos.

## Executar localmente

```bash
npm install
npm start
```

A aplicacao sobe em `http://localhost:4200`.

## Build

```bash
npm run build
```

## Deploy Terraform do frontend

O modulo Terraform para publicar o frontend esta em `front/infra` (S3 + CloudFront).

```powershell
cd front
npm install
npm run build

cd infra
terraform init
terraform plan
terraform apply
```
