﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Devices.Enumeration;
using WinI2c = Windows.Devices.I2c;

namespace System.Device.I2c.Drivers
{
    /// <summary>
    /// Represents an I2C communication channel running on Windows 10 IoT.
    /// </summary>
    public class Windows10I2cDevice : I2cDevice
    {
        private readonly I2cConnectionSettings _settings;
        private WinI2c.I2cDevice _winI2cDevice;

        /// <summary>
        /// Initializes new instance of Windows10I2cDevice that will use the specified settings to communicate with the I2C device.
        /// </summary>
        /// <param name="settings">The connection settings of a device on an I2C bus.</param>
        public Windows10I2cDevice(I2cConnectionSettings settings)
        {
            _settings = settings;
            var winSettings = new WinI2c.I2cConnectionSettings(settings.DeviceAddress);

            string busFriendlyName = $"I2C{settings.BusId}";
            string deviceSelector = WinI2c.I2cDevice.GetDeviceSelector(busFriendlyName);

            DeviceInformationCollection deviceInformationCollection = DeviceInformation.FindAllAsync(deviceSelector).WaitForCompletion();
            if (deviceInformationCollection.Count == 0)
            {
                throw new ArgumentException($"No I2C device exists for bus ID {settings.BusId}.", $"{nameof(settings)}.{nameof(settings.BusId)}");
            }

            _winI2cDevice = WinI2c.I2cDevice.FromIdAsync(deviceInformationCollection[0].Id, winSettings).WaitForCompletion();
            if (_winI2cDevice == null)
            {
                throw new PlatformNotSupportedException($"I2C devices are not supported.");
            }
        }

        /// <summary>
        /// The connection settings of a device on an I2C bus.
        /// </summary>
        public override I2cConnectionSettings ConnectionSettings => _settings;

        /// <summary>
        /// Reads a byte from the I2C device.
        /// </summary>
        /// <returns>A byte read from the I2C device.</returns>
        public override byte ReadByte()
        {
            byte[] buffer = new byte[1];
            _winI2cDevice.Read(buffer);
            return buffer[0];
        }

        /// <summary>
        /// Reads data from the I2C device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read the data from the I2C device.
        /// The length of the buffer determines how much data to read from the I2C device.
        /// </param>
        public override void Read(Span<byte> buffer)
        {
            if (buffer.Length == 0)
                throw new ArgumentException($"{nameof(buffer)} cannot be empty.");

            byte[] byteArray = new byte[buffer.Length];
            _winI2cDevice.Read(byteArray);
            new Span<byte>(byteArray).CopyTo(buffer);
        }

        /// <summary>
        /// Writes a byte to the I2C device.
        /// </summary>
        /// <param name="value">The byte to be written to the I2C device.</param>
        public override void WriteByte(byte value)
        {
            _winI2cDevice.Write(new[] { value });
        }

        /// <summary>
        /// Writes data to the I2C device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that contains the data to be written to the I2C device.
        /// The data should not include the I2C device address.
        /// </param>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _winI2cDevice.Write(buffer.ToArray());
        }

        public override void Dispose(bool disposing)
        {
            _winI2cDevice?.Dispose();
            _winI2cDevice = null;

            base.Dispose(disposing);
        }
    }
}
