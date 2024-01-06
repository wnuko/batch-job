# batch-job

## initial project

```shell
mkdir src
cd src
dotnet new console -f net6.0 --name BatchJob
```

### build project

```shell
cd src\BatchJob
dotnet build
```

### run project

```shell
cd src\BatchJob
dotnet run
```

## create Dockerfile

```shell
cd src\BatchJob
ni Dockerfile
```

### build docker image

```shell
cd src\BatchJob
docker build .
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
