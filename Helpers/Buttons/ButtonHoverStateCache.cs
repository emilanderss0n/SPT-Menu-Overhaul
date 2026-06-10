using EFT.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoxoPixel.MenuOverhaul.Helpers.Buttons
{
    internal struct ButtonHoverBaseState
    {
        public bool HasLabelRect;
        public Vector2 LabelAnchoredPosition;
        public bool HasIconRect;
        public Vector2 IconAnchoredPosition;
        public Vector3 IconScale;
        public Image HoverIndicatorImage;
        public bool HoverIndicatorVisible;
    }

    internal sealed class ButtonHoverStateCache
    {
        private readonly Dictionary<int, ButtonHoverBaseState> _baseStates = new Dictionary<int, ButtonHoverBaseState>();

        public ButtonHoverBaseState GetOrCapture(DefaultUIButtonAnimation instance)
        {
            int id = instance.GetInstanceID();
            if (_baseStates.TryGetValue(id, out ButtonHoverBaseState existing))
            {
                return existing;
            }

            ButtonHoverBaseState state = default;

            if (instance.Label != null && instance.Label.transform is RectTransform labelRect)
            {
                state.HasLabelRect = true;
                state.LabelAnchoredPosition = labelRect.anchoredPosition;
            }

            if (instance.Icon != null)
            {
                state.IconScale = instance.Icon.transform.localScale;
                if (instance.Icon.transform is RectTransform iconRect)
                {
                    state.HasIconRect = true;
                    state.IconAnchoredPosition = iconRect.anchoredPosition;
                }
            }
            else
            {
                state.IconScale = Vector3.one;
            }

            _baseStates[id] = state;
            return state;
        }

        public void Set(DefaultUIButtonAnimation instance, ButtonHoverBaseState state)
        {
            _baseStates[instance.GetInstanceID()] = state;
        }
    }
}
