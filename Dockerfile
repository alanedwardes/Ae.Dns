FROM --platform=linux/arm64/v8 mcr.microsoft.com/dotnet/runtime:3.1

ADD build/output /opt/aedns

VOLUME ["/data"]

ENTRYPOINT ["/opt/aedns/Ae.Dns.Console"]
