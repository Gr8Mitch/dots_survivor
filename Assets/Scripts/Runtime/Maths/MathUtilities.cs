namespace Survivor.Runtime.Maths
{
    using Unity.Mathematics;

    public class MathUtilities
    {
        /// <summary>
        /// Default max length multiplier of reverse projection
        /// </summary>
        public const float DEFAULT_REVERSE_PROJECTION_MAX_LENGTH_RATIO = 10f;
        
        // Picked from the DOTS Character controller package.
        
        /// <summary>
        /// Reorients a vector on a plane while constraining its direction so that the resulting vector is between the original vector and the specified direction
        /// </summary>
        /// <param name="vector"> The original vector to reorient </param>
        /// <param name="onPlaneNormal"> The plane on which the vector should be reoriented </param>
        /// <param name="alongDirection"> The target direction along which the vector should be reoriented </param>
        /// <returns> The reoriented vector </returns>
        public static float3 ReorientVectorOnPlaneAlongDirection(float3 vector, float3 onPlaneNormal, float3 alongDirection)
        {
            float length = math.length(vector);

            if (length <= math.EPSILON)
                return float3.zero;

            float3 reorientAxis = math.cross(vector, alongDirection);
            float3 reorientedVector = math.normalizesafe(math.cross(onPlaneNormal, reorientAxis)) * length;

            return reorientedVector;
        }
        
        /// <summary>
        /// Returns an interpolant parameter that represents interpolating with a given sharpness
        /// </summary>
        /// <param name="sharpness"> The desired interpolation sharpness </param>
        /// <param name="dt"> The interpolation time delta </param>
        /// <returns> The resulting interpolant </returns>
        public static float GetSharpnessInterpolant(float sharpness, float dt)
        {
            return math.saturate(1f - math.exp(-sharpness * dt));
        }
        
        /// <summary>
        /// Builds a rotation that prioritizes having its up direction aligned with the designated up direction. Then, it orients its forwards towards the designated forward direction as much as it can without breaking the primary up direction constraint
        /// </summary>
        /// <param name="up"> The target up direction </param>
        /// <param name="forward"> The target forward direction </param>
        /// <returns> The resulting rotation </returns>
        public static quaternion CreateRotationWithUpPriority(float3 up, float3 forward)
        {
            if (math.abs(math.dot(forward, up)) == 1f)
            {
                forward = math.forward();
            }
            forward = math.normalizesafe(ProjectOnPlane(forward, up));

            return quaternion.LookRotationSafe(forward, up);
        }
        
        /// <summary>
        /// Projects a vector on a plane
        /// </summary>
        /// <param name="vector"> The vector to project </param>
        /// <param name="onPlaneNormal"> The plane normal to project on </param>
        /// <returns> The projected vector </returns>
        public static float3 ProjectOnPlane(float3 vector, float3 onPlaneNormal)
        {
            return vector - math.projectsafe(vector, onPlaneNormal);
        }
        
        /// <summary>
        /// Gets the up direction of a given quaternion
        /// </summary>
        /// <param name="rot"> The rotation in quaternion </param>
        /// <returns> The up direction </returns>
        public static float3 GetUpFromRotation(quaternion rot)
        {
            return math.mul(rot, math.up());
        }
        
        /// <summary>
        /// Calculates the dot product between two normalized direction vectors that are at a specified angle (in radians) from each other
        /// </summary>
        /// <param name="angleRadians"> The angle in radians separating the two fictional direction vectors </param>
        /// <returns> The dot product result </returns>
        public static float AngleRadiansToDotRatio(float angleRadians)
        {
            return math.cos(angleRadians);
        }

        /// <summary>
        /// Calculates a vectorA in the direction of "onNormalizedVector", such that if vectorA was projected on the "projectedVector"'s normalized direction, it would result in "projectedVector"
        /// </summary>
        /// <param name="projectedVector"> The projected vector that we want to de-project </param>
        /// <param name="onNormalizedVector"> The desired normalized direction of the de-projected vector </param>
        /// <param name="maxLength"> The maximum length of the de-projected vector (de-projection can lead to very large or infinite values for near-perpendicular directions) </param>
        /// <returns> The resulting de-projected vector </returns>
        public static float3 ReverseProjectOnVector(float3 projectedVector, float3 onNormalizedVector, float maxLength)
        {
            float projectionRatio = math.dot(math.normalizesafe(projectedVector), onNormalizedVector);
            if (projectionRatio == 0f)
            {
                return projectedVector;
            }

            float deprojectedLength = math.clamp(math.length(projectedVector) / projectionRatio, 0f, maxLength);
            return onNormalizedVector * deprojectedLength;
        }
    }
}