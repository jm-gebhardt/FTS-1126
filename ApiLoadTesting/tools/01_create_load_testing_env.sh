#!/usr/bin/env bash

function generate_password {
  # Generate a random password.
  # $1: Length of the password
  # $2: Number of special characters
  #
  # Inspired by https://stackoverflow.com/a/30948155
  #

  if [ -n "$1" ]; then
    pw_length=$1
  else
    pw_length=10
  fi

  if [ -n "$2" ]; then
    special_char_length=$2
  else
    special_char_length=2
  fi

  if [ ${special_char_length} -gt ${pw_length} ]; then
    echo "special_char_length must be smaller than pw_length"
    exit 1
  fi

  chars='.!,()'
  
  {
    LC_ALL=C grep </dev/urandom -ao '[A-Za-z0-9]' |
      head -n $((${pw_length} - ${special_char_length}))         # Random normal chars
    echo ${chars:$((RANDOM % ${#chars})):${special_char_length}} # Random special char.
  } |
    grep -o . | sort -R |
    tr -d '\n'
}

SCRIPT_DIR=$(dirname "$0")
DOCKER_COMPOSE_DIR=${SCRIPT_DIR}/../../../
ENV_FILE=".env"

cd ${DOCKER_COMPOSE_DIR}

if test -f "${ENV_FILE}"; then
  read -p ".env file already exists. Do you want to remove and recreate it? [Y/N]: " -n 1 -r

  if [[ $REPLY =~ ^[Yy]$ ]]
  then
    rm "${ENV_FILE}"
  else
    echo -e "\nAborted."
    exit 0;
  fi
fi

echo -e "\nCreate .env file..."


cat <<EOF >>${ENV_FILE}
COMPOSE_PROFILES=kaeser,simulation,infrastructure,provisioning,loadtest
COMPOSE_PATH_SEPARATOR=:
COMPOSE_FILE=docker-compose.infrastructure.yml:docker-compose.yml:docker-compose.kaeser.yml:docker-compose.override.yml
COMPOSE_COMPATIBILITY=true
RABBITMQ_USER=guest
RABBITMQ_MANAGEMENTAPIPORT=15672
RABBITMQ_PASSWORD=$(generate_password)

MSSQL_HOST=mssql
MSSQL_DEFAULT_DB_NAME=ffcmain
MSSQL_DEFAULT_USER=ffcdbuser
MSSQL_DEFAULT_USER_PASSWORD=$(generate_password)
MSSQL_SIDECAR_USER=sidecar
MSSQL_SIDECAR_PASSWORD=$(generate_password)
MSSQL_SA_PASSWORD=$(generate_password)
MSSQL_NOTIFICATION_USER=notificationcenterdbuser
MSSQL_NOTIFICATION_USER_PASSWORD=$(generate_password)
MSSQL_DEFAULT_NOTIFICATION_CENTER_DB_NAME=notificationcenterdb
MSSQL_BACKUP_VOLUME_PATH=./tmp/mssql-backups

SQL_PAD_ADMIN_USER=administrator
SQL_PAD_ADMIN_PASSWORD=$(generate_password)

GF_SECURITY_ADMIN_PASSWORD=$(generate_password)

KEYCLOAK_REALM="agv"
KEYCLOAK_IMPORTFILE="keycloak.json"
KEYCLOAK_CLIENT="agv"
KEYCLOAK_CLIENT_SECRET=

KEYCLOAK_USER="admin"
KEYCLOAK_PASSWORD="admin"
KEYCLOAK_PORT="8080"
KEYCLOAK_CLIENTID="agv-kitt"
KEYCLOAK_REALM_USER="agv" 
KEYCLOAK_REALM_USER_PWD="agv"
KEYCLOAK_REALM_SU="agvsupervisor" 
KEYCLOAK_REALM_SU_PWD="agv"
TRANSPORTORDER_API=http://transportorder:80

TRIGGER_FOLDER=triggers
MFRUST_PORT=5006
USTMFR_PORT=5007
MFRUST_HOST="0.0.0.0"
USTMFR_HOST="0.0.0.0"
AGV_CARGO_STATE_PORT=7001
AGV_CARGO_STATE_HOST="129.168.144.70"
APL_WE_STATION_PORT=7001
APL_WE_STATION_HOST="129.168.144.129"
FROM_WE_STATION_PORT=7002
FROM_WE_STATION_HOST="129.168.144.129"
USE_FROM_WE_STATION_CONNECTION=false

STATIONCOMMUNICATION_ARGS=simulate
ORDERTRANSLATOR_ARGS=simulate
WCSCOMMUNICATION_ARGS=
SMARTLINKCOMMUNICATION_ARGS=--url http://10.10.120.1/v1 --pin P-Drill_Pin4 --nodes "P-Drill,D-Press" --waitingTime 5
DEVICECOMMUNICATIONSIMAGV_ARGS=--agvs=5 --karisTypes=3 --spawnAt=map/NODE-17,map/NODE-19,map/NODE-21 --simulationspeed=1 --cargoloadingtime=3 --cargounloadingtime=3 --batterydischargerate=0.02 --batterychargerate=1 --exportedgraph
PRISMATCOMMUNICATION_ARGS=simulate --speed=1
TRANSPORTORDER_ARGS=production --autoAccept
STATISTICS_ARGS=

WEB_SERVER_PORT=80
WEB_SERVER_HOST="http://127.0.0.1"
ALLOWED_ORIGIN="http://127.0.0.1"
EOF

#ENV VARIABLES WITHOUT VARIABLES SUBSTITUTION
cat <<'EOF'>>${ENV_FILE}

WCSCOMMUNICATION_CONNECTIONSTRING_WCS=Server=
WCSCOMMUNICATION_CONNECTIONSTRING_AMC=Server=

DEVICECOMMUNICATION_CONNECTIONSTRING_AMC=

STATISTICS_CONNECTIONSTRING_STAT=Server=${MSSQL_HOST};Database=${MSSQL_DEFAULT_DB_NAME};User Id=${MSSQL_DEFAULT_USER};Password=${MSSQL_DEFAULT_USER_PASSWORD};Encrypt=False

TRANSPORTORDER_CONNECTIONSTRINGS_AMC=Server=${MSSQL_HOST};Database=${MSSQL_DEFAULT_DB_NAME};User Id=${MSSQL_DEFAULT_USER};Password=${MSSQL_DEFAULT_USER_PASSWORD};Encrypt=False
STATIONCOMMUNICATION_CONNECTIONSTRINGS=Server=${MSSQL_HOST};Database=${MSSQL_DEFAULT_DB_NAME};User Id=${MSSQL_DEFAULT_USER};Password=${MSSQL_DEFAULT_USER_PASSWORD};Encrypt=False

TRIGGERSERVICE_CONNECTIONSTRINGS_TRIG=Server=${MSSQL_HOST};Database=${MSSQL_DEFAULT_DB_NAME};User Id=${MSSQL_DEFAULT_USER};Password=${MSSQL_DEFAULT_USER_PASSWORD};Encrypt=False

ORDERTRANSLATOR_CONNECTIONSTRINGS_ORDERTRANSLATOR=Server=${MSSQL_HOST};Database=${MSSQL_DEFAULT_DB_NAME};User Id=${MSSQL_DEFAULT_USER};Password=${MSSQL_DEFAULT_USER_PASSWORD};Encrypt=False

PRISMATCOMMUNICATION_CONNECTIONSTRINGS_PRISMAT=Server=${MSSQL_HOST};Database=${MSSQL_DEFAULT_DB_NAME};User Id=${MSSQL_DEFAULT_USER};Password=${MSSQL_DEFAULT_USER_PASSWORD};Encrypt=False
EOF

echo -e "\n.env file created."
bash bin/19_docker_login.sh

echo -e "\nCopy Kaeser Export provisioning files"
# Provisioning directory path
provisioningDirectory="Config/provisioning/data"
if test -d "$provisioningDirectory"; then
  read -p "Provisioning directory '$provisioningDirectory' exists. Delete this directory? [Y/N]: " -n 1 -r
  if  [[ $REPLY =~ ^[Yy]$ ]]; then
    rm -rf "$provisioningDirectory"
    echo -e "\n'Directory $provisioningDirectory' deleted."
  else
    echo -e "\nDirectory not deleted."
    exit 0;
  fi
fi

sampleTriggerLocationDir="Config/Kaeser/TriggerLocations/TriggerLocations_KaeserExport"
if test -d "$sampleTriggerLocationDir"; then
  configDirectory="Config/provisioning/data/TriggerLocations"
  mkdir -p $configDirectory
  echo "Copy Kaeser Export trigger location provisioning files to '$configDirectory'"
  cp -r $sampleTriggerLocationDir/* $configDirectory
fi

sampleQueuesDir="Config/Kaeser/Queues/Queues_KaeserExport"
if test -d "$sampleQueuesDir"; then
  configDirectory="Config/provisioning/data/Queues"
  mkdir -p $configDirectory
  echo "Copy Kaeser Export queue provisioning files to '$configDirectory'"
  cp -r $sampleQueuesDir/* $configDirectory
fi

sampleNodePropertiesDir="Config/Kaeser/NodeProperties/NodeProperties_KaeserExport"
if test -d "$sampleNodePropertiesDir"; then
  configDirectory="Config/provisioning/data/NodeProperties"
  mkdir -p $configDirectory
  echo "Copy Kaeser Export node properties provisioning files to '$configDirectory'"
  cp -r $sampleNodePropertiesDir/* $configDirectory
fi

echo -e "\nCopy Kaeser Karis export files"
sampleKaeserKarisExportDir="Config/Kaeser/KarisExport"
if test -d "$sampleKaeserKarisExportDir"; then
  configDirectory="Config/KarisExport"
  echo "Copy exported Karis graph files to '$configDirectory' directory"
  read -p "Delete content of '$configDirectory'? [Y/N]: " -n 1 -r
  if [[ $REPLY =~ ^[Yy]$ ]]; then
    rm $configDirectory/*
    echo -e "\nFiles in '$configDirectory' deleted."
  else
    echo -e "\nScript aborted."
    exit 0
  fi 
  echo -e "\nCopy Kaeser Karis export files to '$configDirectory'"
  cp -r $sampleKaeserKarisExportDir/* $configDirectory/

  kaeserGraphConfigurationDir="Config/Kaeser"
  echo "Copy $kaeserGraphConfigurationDir/graph-configuration_Export.yml to $kaeserGraphConfigurationDir/graph-configuration.yml"
  cp $kaeserGraphConfigurationDir/graph-configuration_Export.yml $kaeserGraphConfigurationDir/graph-configuration.yml
fi


stationConfigurationDir="Config/stations"
echo "Copy $kaeserGraphConfigurationDir/station-configuration.yml to $stationConfigurationDir/station-configuration.yml"
cp $kaeserGraphConfigurationDir/station-configuration.yml $stationConfigurationDir/station-configuration.yml

echo "Done"
