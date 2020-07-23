***************************** Application Overview *****************************

The program retrieves raw sensor outputs from InfluxDb, parses it and then export to a Json-file.

To parse raw sensor output the program gets sensor's data type Json-schema from the DMS-system PostgreSQL database.

The application gets sensors outputs for the scpicified time range, Tests and sensors.
For each Test may be specified several different sensors.

Currently InfluxDB and PostrgreSQL connection params are hardcoded in the code. 

	InfluxDB connection params: 

        InfluxHost = "http://localhost:8086";
        DatabaseName = "dms_influx_db".

	PostgreSQl connection params: 

		Host=localhost;
		Port=5432;
		Database=MyDb;
		Username=postgres;
		Password=kurepin.

//TODO: Move connection params to the XML settings file.


***************************** Application Execution *****************************

The pattern for the input params:

 	-directoryPath {directoryPath}  -leftTimeBorder {leftTimeBorder} -rightTimeBorder {rightTimeBorder} -testId {testId} -sensors {sensorIpAddress:sensorPort} [{sensorIp:sensorPort}] [-testId {testId} -sensors {sensorIpAddress:sensorPort} [{sensorIp:sensorPort}]]

  	DateTime must be provided in ISO 8601 format.
	
	 	Example: 2020-05-10T07:32:29.186Z

Examples of application executing:

	SensorOutputParser -directoryPath "exported-files" -leftTimeBorder 2020-05-05T07:32:29.186Z -rightTimeBorder 2020-05-11T19:32:29.251Z -testId 444 -sensors 127.0.0.1:1111 127.0.0.1:2222 -testId 555 -sensors 127.0.0.1:1111 127.0.0.1:2222

	SensorOutputParser -directoryPath "exported-files" -leftTimeBorder 2020-05-05T07:32:29.186Z -rightTimeBorder 2020-05-11T19:32:29.251Z -testId 444 -sensors 127.0.0.1:1111 127.0.0.1:2222 -testId 666 -sensors 127.0.0.1:1111 127.0.0.1:2222


	SensorOutputParser -directoryPath "C:\Users\kurepin\Desktop\path test" -leftTimeBorder 2020-05-05T07:32:29Z -rightTimeBorder 2020-05-11T19:32:29Z -testId 444 -sensors 127.0.0.1:


***************************** Quering InfluxDB *****************************

To search through tags for exact string equality use this pattern: /^{targetValue}}$/

Examples:

	SELECT * FROM sensor_outputs_test_132 WHERE time > 1588985970717778600 AND sensor_ip =~ /^127.0.0.1$/

	SELECT * FROM sensor_outputs_test_132 WHERE time > 1588985970717778600 AND sensor_ip =~ /^127.0.0.1$/ AND sensor_port =~ /^1111$/

	SELECT * FROM sensor_outputs_test_132 WHERE time >= 1588985970717778600 AND time <= 1589024162229517700 AND sensor_ip =~ /^127.0.0.1$/ AND sensor_port =~ /^1111$/

	SELECT * FROM sensor_outputs_test_132 WHERE time >= 1588985973804227400 AND time <= 1589024162229517700 AND sensor_ip =~ /^127.0.0.1$/ AND sensor_port =~ /^1111$/ OR sensor_ip =~ /^127.0.0.1$/ AND sensor_port =~ /^3333$/


***************************** Json Datatype Schema *****************************

Json Datatype Schema must match the specific pattern.

	{
    	   "{fieldName}": "{fieldTypeName}",
    	   "{fieldName}": "{fieldTypeName}",
		...
    	   "{fieldName}": "{fieldTypeName}",
	}

Allowed field type names: 

		int
		double
		string
		bool

Example schema:

	{
    	   "Temperature": "double",
    	   "Moisture": "int",
    	   "IsComfortLevel": "bool",
    	   "Comment": "string"
	}


***************************** Sensors Broadcasts *****************************

Data sending by sensors must encoded to ASCII.

Sensor fields must be separated by comma:

	{fieldValue1},{fieldValue2}, ... ,{fieldValueN}

Example sensor broadcast:
	
	25.4,74,true,Go outside!!!

	OR

	25.4,74,1,Go outside!!!



