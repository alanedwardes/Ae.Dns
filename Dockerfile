FROM mcr.microsoft.com/dotnet/runtime:6.0

ADD build/output /opt/aedns

VOLUME ["/data"]

WORKDIR /data

ENTRYPOINT ["/opt/aedns/Ae.Dns.Console"]
