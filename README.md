# batch-job

Steps:
1. [Initial Project](#initial-project)
   - [Build Project](#build-project)
   - [Run Project](#run-project)
2. [Create Dockerfile](#create-dockerfile)
   - [Build Docker Image](#build-docker-image)
   - [Create Docker Container](#create-docker-container)
   - [Start Docker Container](#start-docker-container)

## Initial Project

```shell
mkdir src
cd src
dotnet new console -f net6.0 --name BatchJob
```

### Build Project

```shell
cd src\BatchJob
dotnet build
```

### Run Project

```shell
cd src\BatchJob
dotnet run
```

## Create Dockerfile

```shell
cd src\BatchJob
ni Dockerfile
```

### build docker image

```shell
cd src\BatchJob
docker build -t batch-job .
docker images
```

### create docker container

```shell
cd src\BatchJob
docker create --name batch-job-run batch-job
docker ps -a
```

### start docker container

```shell
cd src\BatchJob
docker start batch-job-run
```

## AWS

Ensure [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) installation

```shell
aws sts get-caller-identity
```

### create ecr repository

```shell
aws ecr create-repository --repository-name batch-job
```

### push docker image to ecr repository

```shell
aws ecr get-login-password --region {region} | docker login --username AWS --password-stdin {aws_account_id}.dkr.ecr.{region}.amazonaws.com
```

### tag image with ecr registry (assuming tag ':latest')

```shell
docker tag {image_id} {aws_account_id}.dkr.ecr.{region}.amazonaws.com/batch-job
```

### push image to ecr registry (assuming tag ':latest')

```shell
docker push {aws_account_id}.dkr.ecr.{region}.amazonaws.com/batch-job
aws ecr list-images --repository-name batch-job
```

### ECS Fargate

```shell
aws ecs create-cluster --cluster-name batch-job-cluster
```

```shell
aws logs create-log-group --log-group-name batch-job-logs
```

Create a trust policy

```shell
ni ecs-trust-policy.json
```

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Principal": {
                "Service": "ecs-tasks.amazonaws.com"
            },
            "Action": "sts:AssumeRole"
        }
    ]
}
```

If missing, create ecsTaskExecutionRole role

```shell
aws iam create-role --role-name ecsTaskExecutionRole --assume-role-policy-document file://ecs-trust-policy.json --query 'Role.Arn' --output text
aws iam attach-role-policy --role-name ecsTaskExecutionRole --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
```

create batch_job role

```shell
aws iam create-role --role-name batch_job_role --assume-role-policy-document file://ecs-trust-policy.json --query 'Role.Arn' --output text
```

Create an IAM policy

```shell
ni ecs-task-policy.json
```

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "logs",
            "Effect": "Allow",
            "Action": [
                "logs:CreateLogStream"
            ],
            "Resource": "arn:aws:logs:{region}:{aws_account_id}:log-group:batch-job-logs:log-stream:*"
        },
        {
            "Sid": "ecr",
            "Effect": "Allow",
            "Action": [
                "ecr:GetDownloadUrlForLayer",
                "ecr:BatchGetImage"
            ],
            "Resource": "arn:aws:ecr:{region}:{aws_account_id}:repository/batch-job"
        }
    ]
}
```

```shell
aws iam put-role-policy --role-name batch_job_role --policy-name batch_job_policy --policy-document file://ecs-task-policy.json
```

```shell
ni ecs-task-defintion.json
```

```json
{
    "family": "batch-job",
    "taskRoleArn": "arn:aws:iam::{aws_account_id}:role/batch_job_role",
    "executionRoleArn": "arn:aws:iam::{aws_account_id}:role/ecsTaskExecutionRole",
    "networkMode": "awsvpc",
    "containerDefinitions": [
        {
            "name": "batch-container",
            "image": "{repositoryUri}",
            "logConfiguration": {
                "logDriver": "awslogs",
                "options": {
                    "awslogs-group": "batch-job-logs",
                    "awslogs-region": "{region}",
                    "awslogs-stream-prefix": "batch-logs-"
                }
            }
        }], 
    "requiresCompatibilities": ["FARGATE"],
    "cpu": "512",
    "memory": "1024"
}
```

```shell
aws ecs register-task-definition --cli-input-json file://ecs-task-defintion.json
```

```shell
ni ecs-run-task.json
```

```json
{
    "cluster": "batch-job-cluster",
    "count": 1,
    "launchType": "FARGATE",
    "networkConfiguration": {
        "awsvpcConfiguration": {
            "subnets": ["{subnet}"],
            "securityGroups": ["{sg}"],
            "assignPublicIp": "ENABLED"
        }
    },
    "platformVersion": "1.4.0"
}
```

If required, create VPC stack

```shell
aws ec2 create-vpc --cidr-block 10.0.0.0/24 --query Vpc.VpcId --output text
aws ec2 create-internet-gateway --output text --query 'InternetGateway.InternetGatewayId'
aws ec2 describe-route-tables --filters Name=vpc-id,Values={vpc-id} --query RouteTables[0].RouteTableId
aws ec2 attach-internet-gateway --internet-gateway-id {igw-id} --vpc-id {vpc-id}
aws ec2 create-subnet --vpc-id {vpc-id} --cidr-block 10.0.0.0/24 --availability-zone {az} --query Subnet.SubnetId --output text
aws ec2 create-security-group --description "for batch-job" --group-name batch-job-sg --vpc-id {vpc-id} --output text --query 'GroupId'
aws ec2 create-route --route-table-id {rtb-id} --destination-cidr-block 0.0.0.0/0 --gateway-id {igw-id}
```

Execute Task run

```shell
aws ecs run-task --task-definition batch-job:1 --cli-input-json file://ecs-run-task.json
```

Read logs

```shell
aws logs describe-log-streams --log-group-name batch-job-logs --query 'logStreams[0].logStreamName' --output text
aws logs get-log-events --log-group-name batch-job-logs --log-stream-name {stream_name}
```

## Create AWS Batch artefacts

Create Compute Environment ([doc](https://docs.aws.amazon.com/cli/latest/reference/batch/create-compute-environment.html))
(may need more details here, using `--cli-input-json file://compute-environment.json`)
    max vCPU: 1

```shell
aws batch create-compute-environment --compute-environment-name batch-job-ce --type MANAGED --compute-resources type=FARGATE,maxvCpus=1,subnets={sn},securityGroupIds={sg}
```

Create Job Queue ([doc](https://docs.aws.amazon.com/cli/latest/reference/batch/create-job-queue.html))

```shell
aws batch create-job-queue --job-queue-name batch-job-queue --priority 100 --compute-environment-order order=1,computeEnvironment=batch-job-ce
```

Create Job Definition ([doc](https://docs.aws.amazon.com/cli/latest/reference/batch/register-job-definition.html))

```shell
aws batch register-job-definition --job-definition-name batch-job-def --type container --container-properties '{"image":"{aws_account_id}.dkr.ecr.{region}.amazonaws.com/batch-job:latest","resourceRequirements":[{"type":"VCPU","value":"0.5"},{"type":"MEMORY","value":"1024"}],"executionRoleArn":"arn:aws:iam::{aws_account_id}:role/ecsTaskExecutionRole","networkConfiguration":{"assignPublicIp":"ENABLED"}}' --platform-capabilities FARGATE
```

## Test run submitting new Job

Submit new job ([doc](https://docs.aws.amazon.com/cli/latest/reference/batch/submit-job.html))

```shell
aws batch submit-job --job-name 20240110-1234-batch-job --job-queue batch-job-queue --job-definition batch-job-def --container-overrides '{"environment":[{"name":"MESSAGE","value":"Message in the bottle"}]}'
```
