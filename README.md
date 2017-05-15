# docker_shutdown
Demonstration of a wrong container shutdown behavior on Docker for Windows

# How to reproduce?

1. docker build -t shut .
2. docker run -d --name SHUT shut
3. docker restart SHUT
4. docker exec SHUT cmd /c type log.txt
