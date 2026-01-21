using UnityEngine;

public class FeatureGate : MonoBehaviour
{
    [SerializeField] private string requiredFeatureId;
    [SerializeField] private bool hideWhenLocked = true;

    void Start()
    {
        UnlockManager.OnFeatureUnlocked += OnFeatureUnlocked;
        UnlockManager.OnUnlocksChanged += UpdateState;
        UpdateState();
    }

    void OnDestroy()
    {
        UnlockManager.OnFeatureUnlocked -= OnFeatureUnlocked;
        UnlockManager.OnUnlocksChanged -= UpdateState;
    }

    void OnFeatureUnlocked(string featureId)
    {
        if (featureId == requiredFeatureId) UpdateState();
    }

    void UpdateState()
    {
        bool isUnlocked = UnlockManager.Instance != null && 
                          UnlockManager.Instance.IsFeatureUnlocked(requiredFeatureId);
        if (hideWhenLocked) gameObject.SetActive(isUnlocked);
    }
}