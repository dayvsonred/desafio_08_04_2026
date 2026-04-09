variable "aws_region" {
  type    = string
  default = "sa-east-1"
}

variable "project_name" {
  type    = string
  default = "complaint-classifier-phase1"
}

variable "lambda_zip_path" {
  type        = string
  description = "Path to ReceiveComplaint zip package"
}

variable "complaints_table_name" {
  type = string
}

variable "complaints_table_arn" {
  type = string
}

variable "categories_table_name" {
  type = string
}

variable "classification_queue_url" {
  type = string
}

variable "classification_queue_arn" {
  type = string
}

variable "processing_queue_url" {
  type = string
}

variable "messages_bucket_name" {
  type    = string
  default = "itau_desafio_2026"
}

variable "bedrock_model_id" {
  type    = string
  default = "anthropic.claude-3-haiku-20240307-v1:0"
}