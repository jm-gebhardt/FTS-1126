using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
	/// <summary>
	/// AGV Data
	/// </summary>
	public class AgvData : CorrelatedBy<Guid>
	{
		 
		/// <summary>
		///  AGV correlation ID
		/// </summary>
		public Guid CorrelationId { get; }

		/// <summary>
		/// The maximum velocity of the AGV while turning on spot [m/s^2]
		/// </summary>
		public double? Acceleration { get;  }

		/// <summary>
		/// The capacity of the AGVs battery in [Ah]
		/// </summary>
		public double BatteryCapacity { get;  }

		/// <summary>
		/// The IP Address of the AGV
		/// </summary>
		public string? IpV4Address { get;  }

		/// <summary>
		/// The length of the AGV in [m]
		/// </summary>
		public double? Length { get; }

		/// <summary>
		/// The maximum velocity of the AGV in straight direction [m/s]
		/// </summary>
		public double MaxVelocityStraight { get;  }

		/// <summary>
		/// The maximum velocity of the AGV in while turning [m/s]
		/// </summary>
		public double? MaxVelocityTurn { get;  }

		/// <summary>
		/// The maximum velocity of the AGV while turning on spot [rad/s]
		/// </summary>
		public double? MaxVelocityTurnOnSpot { get;  }

		/// <summary>
		/// The model name of the AGV
		/// </summary>
		public string? Model { get;  }

		/// <summary>
		/// The max. payload of the AGV in [kg]
		/// </summary>
		public long? Payload { get; }

		/// <summary>
		/// The list of sensors in the agv
		/// </summary>
		public Sensor[] Sensors { get; }

		/// <summary>
		/// The type of the AGV
		/// </summary>
		public string? Type { get;  }

		/// <summary>
		/// The width of the AGV in [m]
		/// </summary>
		public double? Width { get;  }

		/// <summary>
		/// Agv data
		/// </summary>
		/// <param name="correlationId"></param>
		/// <param name="acceleration"></param>
		/// <param name="batteryCapacity"></param>
		/// <param name="ipV4Address"></param>
		/// <param name="length"></param>
		/// <param name="maxVelocityStraight"></param>
		/// <param name="maxVelocityTurn"></param>
		/// <param name="maxVelocityTurnOnSpot"></param>
		/// <param name="model"></param>
		/// <param name="payload"></param>
		/// <param name="sensors"></param>
		/// <param name="type"></param>
		/// <param name="width"></param>
		public AgvData(Guid correlationId, 
			double? acceleration, 
			double batteryCapacity, 
			string? ipV4Address, 
			double? length, 
			double maxVelocityStraight, 
			double? maxVelocityTurn, 
			double? maxVelocityTurnOnSpot, 
			string? model, 
			long? payload, 
			Sensor[] sensors, 
			string? type, 
			double? width)
		{
			CorrelationId = correlationId;
			Acceleration = acceleration;
			BatteryCapacity = batteryCapacity;
			IpV4Address = ipV4Address;
			Length = length;
			MaxVelocityStraight = maxVelocityStraight;
			MaxVelocityTurn = maxVelocityTurn;
			MaxVelocityTurnOnSpot = maxVelocityTurnOnSpot;
			Model = model;
			Payload = payload;
			Sensors = sensors;
			Type = type;
			Width = width;
		}
		/// <summary>
		/// A sensor object skeleton
		/// </summary>
		[JetBrains.Annotations.PublicAPI]
		public class Sensor
		{
			/// <summary>
			/// The identifier of the sensor
			/// </summary>
			public string? SensorIdentifier { get; set; }
		}
	}
}
