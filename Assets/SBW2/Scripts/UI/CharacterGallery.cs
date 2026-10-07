using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SBW2.Content;
using SBW2.Core;
using SBW2.Data;

namespace SBW2.UI
{
    public class CharacterGallery : MonoBehaviour
    {
        [SerializeField] private RawImage image;
        [SerializeField] private Text caption;

        private PhotoPackLoader loader;
        private CharacterPackManifest pack;
        private string characterId;
        private int index;
        private Texture2D currentTexture;
        private bool loading;

        public int Index => index;
        public int Count => AvailableCount();
        public string CharacterId => characterId;

        int AvailableCount()
        {
            int total = pack?.photos?.Length ?? 0;
            if (total <= 0) return 0;

            var gm = GameplayManager.Instance;
            int unlocked = gm != null
                ? gm.PhotoUnlockedCount(characterId)
                : 1;

            return Mathf.Clamp(unlocked, 0, total);
        }

        private void Awake()
        {
            loader = FindAnyObjectByType<PhotoPackLoader>();
        }

        public void Configure(RawImage targetImage, Text targetCaption)
        {
            image = targetImage;
            caption = targetCaption;
        }

        public void Open(CharacterData character)
        {
            if (character == null || !character.photo_pack_active)
            {
                Clear("No active photo pack.");
                return;
            }

            if (loader == null)
                loader = FindAnyObjectByType<PhotoPackLoader>();

            if (loader == null)
            {
                Clear("Photo loader unavailable.");
                return;
            }

            if (characterId == character.id && loading)
                return;

            if (characterId == character.id && currentTexture != null)
            {
                int available = Mathf.Max(1, Count);
                index = Mathf.Clamp(index, 0, available - 1);
                SetCaption($"{index + 1}/{available} unlocked");
                return;
            }

            StopAllCoroutines();
            loading = true;
            pack = null;
            characterId = character.id;
            index = 0;
            ClearTexture();
            SetCaption("Loading photo…");

            StartCoroutine(loader.LoadPackManifest(
                characterId,
                loaded =>
                {
                    loading = false;
                    pack = loaded;

                    if (pack?.photos != null && AvailableCount() > 0)
                        StartCoroutine(ShowCurrent());
                    else
                    {
                        SetCaption("No photo files in pack.");
                        ClearTexture();
                    }
                },
                error =>
                {
                    loading = false;
                    SetCaption("Photo pack unavailable.");
                    Debug.LogWarning("SBW2 gallery: " + error);
                    ClearTexture();
                }
            ));
        }

        public void Next()
        {
            int count = AvailableCount();
            if (loading || pack?.photos == null || count == 0) return;

            index = (index + 1) % count;
            StartCoroutine(ShowCurrent());
        }

        public void Previous()
        {
            int count = AvailableCount();
            if (loading || pack?.photos == null || count == 0) return;

            index = (index - 1 + count) % count;
            StartCoroutine(ShowCurrent());
        }

        private IEnumerator ShowCurrent()
        {
            int available = AvailableCount();

            if (loader == null || pack?.photos == null || available == 0)
                yield break;

            index = Mathf.Clamp(index, 0, available - 1);

            string requestedCharacter = characterId;
            int requestedIndex = index;
            string file = pack.photos[requestedIndex];
            loading = true;

            yield return loader.LoadTexture(
                requestedCharacter,
                file,
                tex =>
                {
                    loading = false;

                    if (requestedCharacter != characterId || requestedIndex != index)
                    {
                        if (tex != null) Destroy(tex);
                        return;
                    }

                    ClearTexture();
                    currentTexture = tex;

                    if (image != null)
                    {
                        image.texture = tex;
                        image.enabled = true;
                        FitImage(tex);
                    }

                    SetCaption(
                        $"{index + 1}/{AvailableCount()} unlocked · " +
                        $"{pack.photos.Length} total"
                    );
                },
                error =>
                {
                    loading = false;

                    if (requestedCharacter != characterId)
                        return;

                    SetCaption("Photo unavailable.");
                    Debug.LogWarning("SBW2 gallery: " + error);
                    ClearTexture();
                }
            );
        }

        private void FitImage(Texture tex)
        {
            if (image == null || tex == null) return;

            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = image.gameObject.AddComponent<AspectRatioFitter>();

            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)tex.width / tex.height;
        }

        private void ClearTexture()
        {
            if (image != null)
            {
                image.texture = null;
                image.enabled = false;
            }

            if (currentTexture != null)
            {
                Destroy(currentTexture);
                currentTexture = null;
            }
        }

        private void SetCaption(string value)
        {
            if (caption != null)
                caption.text = value;
        }

        public void Clear(string message = "")
        {
            StopAllCoroutines();
            loading = false;
            pack = null;
            characterId = null;
            index = 0;
            ClearTexture();
            SetCaption(message);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            ClearTexture();
        }
    }
}
