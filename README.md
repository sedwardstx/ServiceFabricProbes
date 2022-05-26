# ServiceFabricProbes
Examples of Service Fabric HTTP Readiness and Liveness probes

**Website1:** does not use any Service Fabric Probe, relys on HTTP probe defined on the Load Balancer

LB Probe definition

```json
      {
        "frontendPort": 8818,
        "backendPort": 8818,
        "protocol": "tcp",
        "probeProtocol": "http",
        "probeRequestPath": "/api/Health"
      }
```

**WebSite2** code package policy

```XML
      <CodePackagePolicy CodePackageRef="Code">
          <!-- The Probe below is a readiness probe.  The / endpoint in this example will return 400 Badrequest while the
         cache is being populated, then will return 200 OK, this will cause the endpoint to be unavilable until the cache 
         has been fully populated. -->
        <Probes>
          <Probe Type="Readiness" TimeoutSeconds="20" InitialDelaySeconds="150" FailureThreshold="5" SuccessThreshold="2">
            <HttpGet Path="/" EndpointRef="ServiceEndpoint">
              <HttpHeader Name="EndpointReadinessStatus" Value="probing" />
            </HttpGet>
          </Probe>
        </Probes>
      </CodePackagePolicy>
```

**WebSite3** code package policy

```XML
      <CodePackagePolicy CodePackageRef="Code">
          <!-- The Probe below is a liveness probe.  The /api/HealthCheck endpoint in this example is always returning
         400 bad request, so this will cause the code package to get restarted every 1.5 minutes. -->
        <Probes>
          <Probe Type="Liveness" TimeoutSeconds="20" PeriodSeconds="30" InitialDelaySeconds="10" FailureThreshold="5" SuccessThreshold="2">
            <HttpGet Path="/api/HealthCheck" EndpointRef="ServiceEndpoint">
              <HttpHeader Name="EndpointReadinessStatus" Value="probing" />
            </HttpGet>
          </Probe>
        </Probes>
      </CodePackagePolicy>
```
