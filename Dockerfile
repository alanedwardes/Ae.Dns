FROM mcr.microsoft.com/dotnet/runtime:6.0

ARG TARGETPLATFORM

ADD build/$TARGETPLATFORM /opt/aedns

VOLUME ["/data"]

WORKDIR /data

ENTRYPOINT ["/opt/aedns/Ae.Dns.Console"]
