variable "aws_region" {
  type        = string
  description = "AWS region"
  default     = "sa-east-1"
}

variable "principal_bucket_name" {
  type        = string
  description = "Central S3 bucket name for the project"
  default     = "itau-data-teste-20260411"
}

variable "folder_prefixes" {
  type        = list(string)
  description = "Folder-like prefixes to create in the principal bucket"
  default = [
    "complaint_message_received",
    "complaint_message_processed",
    "frontend"
  ]
}

variable "tags" {
  type        = map(string)
  description = "Additional tags for S3 resources"
  default     = {}
}
