using UnityEngine;

namespace Script.View
{
    public class CargoView : CardView
    {
        [SerializeReference]
        float randPosAroundRatio = 2f;
        public override void OnClick()
        {
            // Spawn cards around cargo on X-Z plane (top-down view)
            var RandPosAround = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized * randPosAroundRatio;
            RandPosAround += this.transform.position;
            GamePlayManager.Instance.AddCard( ((CargoCard)thisCard).PopCard(), RandPosAround);
        }
    }
}