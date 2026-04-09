terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

resource "aws_dynamodb_table" "complaints" {
  name         = "${var.project_name}-complaints"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "PK"
  range_key    = "SK"

  attribute {
    name = "PK"
    type = "S"
  }

  attribute {
    name = "SK"
    type = "S"
  }

  tags = {
    Service = "complaint-classifier"
    Domain  = "complaints"
    Phase   = "1"
  }
}

resource "aws_dynamodb_table" "categories" {
  name         = "${var.project_name}-categories"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "PK"
  range_key    = "SK"

  attribute {
    name = "PK"
    type = "S"
  }

  attribute {
    name = "SK"
    type = "S"
  }

  tags = {
    Service = "complaint-classifier"
    Domain  = "categories"
    Phase   = "1"
  }
}

resource "aws_dynamodb_table_item" "category_imobiliario" {
  table_name = aws_dynamodb_table.categories.name
  hash_key   = aws_dynamodb_table.categories.hash_key
  range_key  = aws_dynamodb_table.categories.range_key

  item = jsonencode({
    PK          = { S = "CATEGORY#imobiliario" }
    SK          = { S = "METADATA" }
    name        = { S = "imobiliario" }
    keywords    = { L = [{ S = "credito imobiliario" }, { S = "casa" }, { S = "apartamento" }] }
    description = { S = "Assuntos relacionados a financiamento ou credito imobiliario." }
  })
}

resource "aws_dynamodb_table_item" "category_seguros" {
  table_name = aws_dynamodb_table.categories.name
  hash_key   = aws_dynamodb_table.categories.hash_key
  range_key  = aws_dynamodb_table.categories.range_key

  item = jsonencode({
    PK          = { S = "CATEGORY#seguros" }
    SK          = { S = "METADATA" }
    name        = { S = "seguros" }
    keywords    = { L = [{ S = "resgate" }, { S = "capitalizacao" }, { S = "socorro" }] }
    description = { S = "Demandas relacionadas a seguros, assistencia e capitalizacao." }
  })
}

resource "aws_dynamodb_table_item" "category_cobranca" {
  table_name = aws_dynamodb_table.categories.name
  hash_key   = aws_dynamodb_table.categories.hash_key
  range_key  = aws_dynamodb_table.categories.range_key

  item = jsonencode({
    PK          = { S = "CATEGORY#cobranca" }
    SK          = { S = "METADATA" }
    name        = { S = "cobranca" }
    keywords    = { L = [{ S = "fatura" }, { S = "cobranca" }, { S = "valor" }, { S = "indevido" }] }
    description = { S = "Problemas relacionados a cobrancas e divergencia de valores." }
  })
}

resource "aws_dynamodb_table_item" "category_acesso" {
  table_name = aws_dynamodb_table.categories.name
  hash_key   = aws_dynamodb_table.categories.hash_key
  range_key  = aws_dynamodb_table.categories.range_key

  item = jsonencode({
    PK          = { S = "CATEGORY#acesso" }
    SK          = { S = "METADATA" }
    name        = { S = "acesso" }
    keywords    = { L = [{ S = "acessar" }, { S = "login" }, { S = "senha" }] }
    description = { S = "Problemas para entrar na conta e autenticar." }
  })
}

resource "aws_dynamodb_table_item" "category_aplicativo" {
  table_name = aws_dynamodb_table.categories.name
  hash_key   = aws_dynamodb_table.categories.hash_key
  range_key  = aws_dynamodb_table.categories.range_key

  item = jsonencode({
    PK          = { S = "CATEGORY#aplicativo" }
    SK          = { S = "METADATA" }
    name        = { S = "aplicativo" }
    keywords    = { L = [{ S = "app" }, { S = "aplicativo" }, { S = "travando" }, { S = "erro" }] }
    description = { S = "Falhas tecnicas no app, travamentos e instabilidade." }
  })
}

resource "aws_dynamodb_table_item" "category_fraude" {
  table_name = aws_dynamodb_table.categories.name
  hash_key   = aws_dynamodb_table.categories.hash_key
  range_key  = aws_dynamodb_table.categories.range_key

  item = jsonencode({
    PK          = { S = "CATEGORY#fraude" }
    SK          = { S = "METADATA" }
    name        = { S = "fraude" }
    keywords    = { L = [{ S = "fatura" }, { S = "nao reconhece divida" }, { S = "fraude" }] }
    description = { S = "Situacoes de possivel fraude e transacoes nao reconhecidas." }
  })
}
