namespace Project.Interaction
{
    /// <summary>
    /// Sahnedeki her etkileşilebilir nesnenin sözleşmesi.
    /// Delil, yağ kutusu, gizli kaset, kapı, telefon... hepsi bu arayüzü uygular.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Bakılırken ekranda gösterilecek yazı. Örn: "İncele: Kilit Etiketi"</summary>
        string InteractionPrompt { get; }

        /// <summary>Şu an etkileşim geçerli mi? (Örn: zaten toplanmış delile tekrar basılamaz)</summary>
        bool CanInteract { get; }

        /// <summary>Oyuncu E'ye bastığında çalışır.</summary>
        void Interact(Interactor interactor);
    }
}
