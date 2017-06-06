# Example to compare dynamically created services to existing services.

Dynamically created services allow for better cluster utilization and enable resource management by caller. 

The downside however is the activation time of a dynamically created service. 

The code is in this repo was derived from an early POC of the [WK project](https://blogs.msdn.microsoft.com/azureservicefabric/2017/05/08/service-fabric-customer-profile-wolters-kluwer-cch/)

# Usage
1. Build and deploy all 3 solutions
- Scheulders.sln
- Compute2.sln
- Metrics.sln
2. Double Check deployment in [SF Explorer](http://localhost:19080). You should see 3 applications.
- Compute2Type
- LogKeeperType
- SchedulerAppType
3. Bring up the log viewer. It's at [http://localhost:8084/](http://localhost:8084/) on a OneBox cluster
4. Create a job running in a Dynamic Service Instance. 
- Find the endpoint at SchedulerAppType ->  fabric:/SchedulerApp/DynamicSchedulerService -> PARTITION -> NODE
- `curl` / `Invoke-WebRequest` to endpointURL/api/Dynamic/GetNewJob
5. Create a job running in a partition  pre-allocated (static) service p
- `curl` / `Invoke-WebRequest` to http://localhost:8082/api/Dynamic/GetNewJob/[DURATON]
6. Observe where the job us running and note activation time in the [LogViewer](http://localhost:8084/)
7. repeat with different duration values

# TODO
1. Clean Up ASPNETCore ... align with latest template
2. Refactor services / API projects where needed
3. add servince instance auto scale
4. Docs for different services and APIs
5. Redo logview with async rather than refresh
6. API callers?
7. dynamic stateful endpoint without partition?
8. Latest SF SDK
9. Documentaition
10. Better log viewer




