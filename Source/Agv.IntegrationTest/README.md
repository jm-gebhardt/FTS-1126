# IntegrationTest
The integration test project starts up the entire system (in docker), adds Agvs and creates action groups.

The TransportOrder Rest client is automatically generated during build based on the Api/transportorder_swagger.json.
When the Api specification changes you have to download the new swagger to that folder.

## Local debugging
There is a IntegrationTest.LocalDebug project. This allows you to run a unit test manually.
Required environment variables:
(Examples values based on "C:\w\AgvDevComb" as an example root repository path)
COMPOSE_FILE_IntegrationTest="C:\w\AgvDevComb\docker-compose.yml", "C:\w\AgvDevComb\docker-compose.infrastructure.yml"
COMPOSE_FILE_VirtualAGV="C:\w\AgvDevComb\docker-compose.karis.yml"
DOCKER_REGISTRY_PASSWORD
DOCKER_REGISTRY_REPOSITORY=registry.gitlab.com/tar4/karis_virtual
DOCKER_REGISTRY_URL=crffcpocarch.azurecr.io
DOCKER_REGISTRY_USER=fleet-management-cr
KEYCLOAK_DEFAULT_CLIENT_ID=agv-kitt
KEYCLOAK_DEFAULT_USERNAME=agv
KEYCLOAK_DEFAULT_USERPASSWORD=agv
MQTT_SAMPLES=C:\w\AgvDevComb\ACC\Doku\examples

