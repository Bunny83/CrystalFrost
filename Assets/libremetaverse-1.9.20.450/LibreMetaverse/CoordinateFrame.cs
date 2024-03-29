/*
 * Copyright (c) 2006-2016, openmetaverse.co
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.co nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;

namespace OpenMetaverse
{
    public class CoordinateFrame
    {
        public static readonly OMVVector3 X_AXIS = new OMVVector3(1f, 0f, 0f);
        public static readonly OMVVector3 Y_AXIS = new OMVVector3(0f, 1f, 0f);
        public static readonly OMVVector3 Z_AXIS = new OMVVector3(0f, 0f, 1f);

        /// <summary>Origin position of this coordinate frame</summary>
        public OMVVector3 Origin
        {
            get { return origin; }
            set
            {
                if (!value.IsFinite())
                    throw new ArgumentException("Non-finite in CoordinateFrame.Origin assignment");
                origin = value;
            }
        }
        /// <summary>X axis of this coordinate frame, or Forward/At in grid terms</summary>
        public OMVVector3 XAxis
        {
            get { return xAxis; }
            set
            {
                if (!value.IsFinite())
                    throw new ArgumentException("Non-finite in CoordinateFrame.XAxis assignment");
                xAxis = value;
            }
        }
        /// <summary>Y axis of this coordinate frame, or Left in grid terms</summary>
        public OMVVector3 YAxis
        {
            get { return yAxis; }
            set
            {
                if (!value.IsFinite())
                    throw new ArgumentException("Non-finite in CoordinateFrame.YAxis assignment");
                yAxis = value;
            }
        }
        /// <summary>Z axis of this coordinate frame, or Up in grid terms</summary>
        public OMVVector3 ZAxis
        {
            get { return zAxis; }
            set
            {
                if (!value.IsFinite())
                    throw new ArgumentException("Non-finite in CoordinateFrame.ZAxis assignment");
                zAxis = value;
            }
        }

        protected OMVVector3 origin;
        protected OMVVector3 xAxis;
        protected OMVVector3 yAxis;
        protected OMVVector3 zAxis;

        #region Constructors

        public CoordinateFrame(OMVVector3 origin)
        {
            this.origin = origin;
            xAxis = X_AXIS;
            yAxis = Y_AXIS;
            zAxis = Z_AXIS;

            if (!this.origin.IsFinite())
                throw new ArgumentException("Non-finite in CoordinateFrame constructor");
        }

        public CoordinateFrame(OMVVector3 origin, OMVVector3 direction)
        {
            this.origin = origin;
            LookDirection(direction);

            if (!IsFinite())
                throw new ArgumentException("Non-finite in CoordinateFrame constructor");
        }

        public CoordinateFrame(OMVVector3 origin, OMVVector3 xAxis, OMVVector3 yAxis, OMVVector3 zAxis)
        {
            this.origin = origin;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.zAxis = zAxis;

            if (!IsFinite())
                throw new ArgumentException("Non-finite in CoordinateFrame constructor");
        }

        public CoordinateFrame(OMVVector3 origin, Matrix4 rotation)
        {
            this.origin = origin;
            xAxis = rotation.AtAxis;
            yAxis = rotation.LeftAxis;
            zAxis = rotation.UpAxis;

            if (!IsFinite())
                throw new ArgumentException("Non-finite in CoordinateFrame constructor");
        }

        public CoordinateFrame(OMVVector3 origin, OMVQuaternion rotation)
        {
            Matrix4 m = Matrix4.CreateFromQuaternion(rotation);

            this.origin = origin;
            xAxis = m.AtAxis;
            yAxis = m.LeftAxis;
            zAxis = m.UpAxis;

            if (!IsFinite())
                throw new ArgumentException("Non-finite in CoordinateFrame constructor");
        }

        #endregion Constructors

        #region Public Methods

        public void ResetAxes()
        {
            xAxis = X_AXIS;
            yAxis = Y_AXIS;
            zAxis = Z_AXIS;
        }

        public void Rotate(float angle, OMVVector3 rotationAxis)
        {
            OMVQuaternion q = OMVQuaternion.CreateFromAxisAngle(rotationAxis, angle);
            Rotate(q);
        }

        public void Rotate(OMVQuaternion q)
        {
            Matrix4 m = Matrix4.CreateFromQuaternion(q);
            Rotate(m);
        }

        public void Rotate(Matrix4 m)
        {
            xAxis = OMVVector3.Transform(xAxis, m);
            yAxis = OMVVector3.Transform(yAxis, m);

            Orthonormalize();

            if (!IsFinite())
                throw new Exception("Non-finite in CoordinateFrame.Rotate()");
        }

        public void Roll(float angle)
        {
            OMVQuaternion q = OMVQuaternion.CreateFromAxisAngle(xAxis, angle);
            Matrix4 m = Matrix4.CreateFromQuaternion(q);
            Rotate(m);

            if (!yAxis.IsFinite() || !zAxis.IsFinite())
                throw new Exception("Non-finite in CoordinateFrame.Roll()");
        }

        public void Pitch(float angle)
        {
            OMVQuaternion q = OMVQuaternion.CreateFromAxisAngle(yAxis, angle);
            Matrix4 m = Matrix4.CreateFromQuaternion(q);
            Rotate(m);

            if (!xAxis.IsFinite() || !zAxis.IsFinite())
                throw new Exception("Non-finite in CoordinateFrame.Pitch()");
        }

        public void Yaw(float angle)
        {
            OMVQuaternion q = OMVQuaternion.CreateFromAxisAngle(zAxis, angle);
            Matrix4 m = Matrix4.CreateFromQuaternion(q);
            Rotate(m);

            if (!xAxis.IsFinite() || !yAxis.IsFinite())
                throw new Exception("Non-finite in CoordinateFrame.Yaw()");
        }

        public void LookDirection(OMVVector3 at)
        {
            LookDirection(at, Z_AXIS);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="at">Looking direction, must be a normalized vector</param>
        /// <param name="upDirection">Up direction, must be a normalized vector</param>
        public void LookDirection(OMVVector3 at, OMVVector3 upDirection)
        {
            // The two parameters cannot be parallel
            OMVVector3 left = OMVVector3.Cross(upDirection, at);
            if (left == OMVVector3.Zero)
            {
                // Prevent left from being zero
                at.X += 0.01f;
                at.Normalize();
                left = OMVVector3.Cross(upDirection, at);
            }
            left.Normalize();

            xAxis = at;
            yAxis = left;
            zAxis = OMVVector3.Cross(at, left);
        }

        /// <summary>
        /// Align the coordinate frame X and Y axis with a given rotation
        /// around the Z axis in radians
        /// </summary>
        /// <param name="heading">Absolute rotation around the Z axis in
        /// radians</param>
        public void LookDirection(double heading)
        {
            yAxis.X = (float)Math.Cos(heading);
            yAxis.Y = (float)Math.Sin(heading);
            xAxis.X = (float)-Math.Sin(heading);
            xAxis.Y = (float)Math.Cos(heading);
        }

        public void LookAt(OMVVector3 origin, OMVVector3 target)
        {
            LookAt(origin, target, new OMVVector3(0f, 0f, 1f));
        }

        public void LookAt(OMVVector3 origin, OMVVector3 target, OMVVector3 upDirection)
        {
            this.origin = origin;
            OMVVector3 at = new OMVVector3(target - origin);
            at.Normalize();

            LookDirection(at, upDirection);
        }

        #endregion Public Methods

        protected bool IsFinite()
        {
            if (xAxis.IsFinite() && yAxis.IsFinite() && zAxis.IsFinite())
                return true;
            else
                return false;
        }

        protected void Orthonormalize()
        {
            // Make sure the axis are orthagonal and normalized
            xAxis.Normalize();
            yAxis -= xAxis * (xAxis * yAxis);
            yAxis.Normalize();
            zAxis = OMVVector3.Cross(xAxis, yAxis);
        }
    }
}
