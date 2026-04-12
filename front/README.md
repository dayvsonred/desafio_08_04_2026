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

O modulo Terraform para publicar o frontend esta em `front/infra` (CloudFront + upload no bucket central).

```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\s3\terraform
terraform init
terraform apply

cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\front
npm install
npm run build

cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\front\infra
terraform init
terraform plan
terraform apply
```

Bucket central usado por padrao:
- `itau-data-teste-20260411`
- prefix do frontend: `frontend/`

terraform output -raw frontend_url


cloudfront_distribution_id = "E3NIOFEQPP4VRW"
cloudfront_domain_name = "d3dbpxffvde3hb.cloudfront.net"
frontend_bucket_name = "itau-data-teste-20260411"
frontend_prefix = "frontend"
frontend_url = "https://d3dbpxffvde3hb.cloudfront.net"
uploaded_files_count = 7