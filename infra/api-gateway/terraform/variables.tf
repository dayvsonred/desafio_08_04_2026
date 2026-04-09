variable "aws_region" {
  type        = string
  description = "AWS region"
  default     = "sa-east-1"
}

variable "project_name" {
  type        = string
  description = "Project name prefix"
  default     = "complaint-classifier-phase1"
}

variable "receive_lambda_invoke_arn" {
  type        = string
  description = "Invoke ARN from ReceiveComplaint lambda"
}

variable "receive_lambda_name" {
  type        = string
  description = "Function name from ReceiveComplaint lambda"
}

variable "metrics_lambda_invoke_arn" {
  type        = string
  description = "Invoke ARN from DailyComplaintMetrics lambda"
}

variable "metrics_lambda_name" {
  type        = string
  description = "Function name from DailyComplaintMetrics lambda"
}
