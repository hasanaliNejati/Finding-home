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
            
            // موقعیت شروع = موقعیت Cargo (برای انیمیشن)
            Vector3 startPosition = this.transform.position;
            
            var newCard = ((CargoCard)thisCard).PopCard();
            newCard.Position = RandPosAround; // موقعیت نهایی
            newCard.Id = GamePlayManager.Instance.GetNextCardId();
            GamePlayManager.Instance.Cards.Add(newCard.Id, newCard);
            
            // کارت را روی Cargo بساز تا با انیمیشن به موقعیت نهایی برود
            GamePlayManager.Instance.GenerateVisualCard(newCard, startPosition);
            GamePlayManager.Instance.RefreshDeletedCards();
        }
    }
}