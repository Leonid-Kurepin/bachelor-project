***************************** Application Overview *****************************

The program listens to port 8888 and recives UDP packages.

When the package is received from one of the specified sensors, received data with sensor info is written to the InfluxDB measurement.

Sensor is defined by its Ip-address and Port.

For each 'Test' (in definitions of developing DMS-system) there is a separate InfluxDB measurement where sensors outputs are written and stored.

    Measurements name patter:

            'sensor_outputs_test_{testId}'

Currently InfluxDB connection params are hardcoded in the code. 
    
    InfluxDB connection params: 

        InfluxHost = "http://localhost:8086";
        DatabaseName = "dms_influx_db";

//TODO: Move InfluxDB connection params to the XML settings file.


***************************** Application Execution *****************************

The pattern for the input params:

  -testId {testId} -executionTime {executionTime} -sensors {sensorIpAddress:sensorPort} [{sensorIp:sensorPort}]

Examples of application executing:

    SensorListener -testId 132 -executionTime 5 -sensors 127.0.0.1:1111 127.0.0.1:2222 127.2.2.2:3333

    SensorListener -testId 132 -executionTime 20 -sensors 127.0.0.1:1111 127.0.0.1:2222 127.2.2.2:3333

    SensorListener -testId 444 -executionTime 2000 -sensors 127.0.0.1:1111 127.0.0.1:2222

        SensorListener -testId 444 -executionTime 5 -sensors 127.0.6666.1:1111 127.0.0.1:2222

        SensorListener -testId 165 -executionTime 3000 -sensors 127.0.0.3:55555 


