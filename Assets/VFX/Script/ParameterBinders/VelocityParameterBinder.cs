using UnityEngine.VFX.Utility;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Utility/Velocity")]
    public class VelocityParameterBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty VelocityParameter = "OwnerVelocity";

        Vector3 velocity;
        Vector3 oldPosition;

        protected override void OnEnable()
        {
            base.OnEnable();
            oldPosition = gameObject.transform.position;
        }

        public override bool IsValid(VisualEffect component)
        {
            return component.HasVector3(VelocityParameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 position = gameObject.transform.position;
            velocity = (oldPosition - position) * Time.deltaTime;

            component.SetVector3(VelocityParameter, velocity);

            oldPosition = position;
        }

        public override string ToString()
        {
            return "Velocity : " + VelocityParameter.ToString();
        }
    }

}
