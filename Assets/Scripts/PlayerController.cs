using UnityEngine;
using System;
using System.Net.Sockets;
using UnityEditor.Experimental.GraphView;
using System.Net;
using System.Net.Sockets;
using System.Text;

public enum AnimalForm { Frog, Mouse, Cow, Cat, Chicken, Seagull }

[Serializable]
public class Animal
{
    public AnimalForm form;
    public GameObject visual;
    public Vector2 scale = Vector2.one;
    public float speed = 0.5f;
}

public class PlayerController : MonoBehaviour
{
    public GameObject frogPrefab;
    public GameObject mousePrefab;
    public GameObject cowPrefab;
    public GameObject catPrefab;
    public GameObject chickenPrefab;
    public GameObject seagullPrefab;

    public Transform visualHolder;

    public Animal currentAnimal { get; private set; }

    private Animal[] animals;
    public AnimalForm newForm = AnimalForm.Cow;
    private BoxCollider2D boxCollider;
    private GameObject currentVisual;
    UdpClient udpClient;
    int port = 5005;
    private bool changingForm;

    void Awake()
    {
        animals = new Animal[]
        {
            new Animal { form = AnimalForm.Frog, visual = frogPrefab, scale = Vector2.one, speed = 1.2f },
            new Animal { form = AnimalForm.Mouse, visual = mousePrefab, scale = Vector2.one, speed = 1.3f },
            new Animal { form = AnimalForm.Cow, visual = cowPrefab, scale = Vector2.one, speed = 1.7f },
            new Animal { form = AnimalForm.Cat, visual = catPrefab, scale = Vector2.one, speed = 2.5f },
            new Animal { form = AnimalForm.Chicken, visual = chickenPrefab, scale = Vector2.one, speed = -0.2f },
            new Animal { form = AnimalForm.Seagull, visual = seagullPrefab, scale = Vector2.one, speed = 1.1f }
        };
    }

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        ChangeForm(AnimalForm.Cow);
        udpClient = new UdpClient(port);
        udpClient.BeginReceive(OnReceive, null);
        Debug.Log("Listening for audio classification results...");
    }

    void Update()
    {
        if (currentAnimal != null)
            transform.Translate(Vector2.right * currentAnimal.speed * Time.deltaTime);
        if (changingForm)
            return;
        if (currentAnimal.form != newForm && newForm == AnimalForm.Cow)
            ChangeForm(AnimalForm.Cow);
        if (currentAnimal.form != newForm && newForm == AnimalForm.Cat)
            ChangeForm(AnimalForm.Cat);
        if (currentAnimal.form != newForm && newForm == AnimalForm.Chicken)
            ChangeForm(AnimalForm.Chicken);
        if (currentAnimal.form != newForm && newForm == AnimalForm.Seagull)
            ChangeForm(AnimalForm.Seagull);
        if (currentAnimal.form != newForm && newForm == AnimalForm.Frog)
            ChangeForm(AnimalForm.Frog);
        if (currentAnimal.form != newForm && newForm == AnimalForm.Mouse)
            ChangeForm(AnimalForm.Mouse);

    }

    public void ChangeForm(AnimalForm newForm)
    {
        if ( visualHolder != null)
        {
            Destroy(currentVisual);
        }
        transform.position += Vector3.left * 1.5f; // Move the player slightly to the right when changing form
        foreach (var animal in animals)
        {
            Debug.Log("In Foreach" + animal.ToString());
            if (animal.form == newForm)
            {
                currentAnimal = animal;

                // Apply scale to collider size only
                if (boxCollider != null)
                    boxCollider.size = animal.scale;

                // Replace visual
                currentVisual = Instantiate(animal.visual, visualHolder);
                currentVisual.transform.localPosition = Vector3.zero;

                break;
            }
        }
        changingForm = false;
    }
    void OnReceive(System.IAsyncResult result)
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
        byte[] data = udpClient.EndReceive(result, ref endPoint);

        string message = Encoding.UTF8.GetString(data);
        Debug.Log($"Received: {message}");
        // split message in the comma 

        string form = message;//.Split(',')[0];
        //int confidence = int.Parse(message.Split(',')[1]);

        // Parse the message to determine the animal form
        //Enum.TryParse(form, true, out newForm);

        if (form == "Cow")
            newForm = AnimalForm.Cow;
        else if (form == "Mouse")
            newForm = AnimalForm.Mouse;
        else if (form == "Seagull")
            newForm = AnimalForm.Seagull;
        else if (form == "Frog")
            newForm = AnimalForm.Frog;
        else if (form == "Chicken")
            newForm = AnimalForm.Chicken;
        else if (form == "Cat")
            newForm = AnimalForm.Cat;
        else
            Debug.Log("Did not ecognize form"); // Default to Cow if unrecognized

        udpClient.BeginReceive(OnReceive, null);
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();

    }
    internal void Stopped()
    {
        transform.position += Vector3.left * 0.5f;
    }
}
