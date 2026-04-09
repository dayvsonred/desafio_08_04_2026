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

variable "classification_visibility_timeout_seconds" {
  type        = number
  default     = 120
  description = "Visibility timeout for classification queue"
}

variable "processing_visibility_timeout_seconds" {
  type        = number
  default     = 120
  description = "Visibility timeout for processing queue"
}

