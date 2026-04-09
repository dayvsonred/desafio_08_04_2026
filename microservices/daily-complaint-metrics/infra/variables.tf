variable "aws_region" {
  type    = string
  default = "sa-east-1"
}

variable "project_name" {
  type    = string
  default = "complaint-classifier-phase1"
}

variable "lambda_zip_path" {
  type = string
}

variable "daily_metrics_table_name" {
  type = string
}

variable "daily_metrics_table_arn" {
  type = string
}

variable "metrics_queue_arn" {
  type = string
}
