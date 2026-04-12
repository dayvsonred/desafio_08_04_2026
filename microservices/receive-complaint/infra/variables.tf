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

variable "messages_bucket_name" {
  type    = string
  default = "itau-data-teste-20260411"
}

variable "bedrock_model_id" {
  type    = string
  default = "anthropic.claude-3-haiku-20240307-v1:0"
}
