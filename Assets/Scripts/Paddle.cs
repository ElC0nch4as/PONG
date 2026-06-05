using UnityEngine;

public class Paddle : MonoBehaviour
{
    public enum MoveAxis
    {
        Vertical,
        Horizontal
    }

    [Header("Movement")]
    [SerializeField] private MoveAxis axis = MoveAxis.Vertical;
    [SerializeField] private float speed = 10f;

    [Header("Clamp")]
    [SerializeField] private float min = -4f;
    [SerializeField] private float max = 4f;

    private float moveInput;

    public float Speed
    {
        get { return speed; }
    }

    public void SetInput(float input)
    {
        moveInput = input;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetClamp(float newMin, float newMax)
    {
        min = newMin;
        max = newMax;
    }
    public float MoveInput => moveInput;

    private void Update()
    {
        Vector3 position = transform.position;

        if (axis == MoveAxis.Vertical)
        {
            position.y += moveInput * speed * Time.deltaTime;
            position.y = Mathf.Clamp(position.y, min, max);
        }
        else
        {
            position.x += moveInput * speed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, min, max);
        }

        transform.position = position;
    }
}