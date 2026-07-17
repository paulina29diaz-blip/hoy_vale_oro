using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public float velocidad = 5f;
    public float aceleracion = 2f;
    public float tiempoFrenado = 1f;
    private Rigidbody2D rb;
    private float velocidadActual = 0f;

    private static Vector3? posicionGuardada = null;

    [Header("Ruedas Visuales (Giro)")]
    public Sprite spriteRueda;
    public float multiplicadorGiro = 150f; // Grados de rotación por unidad de velocidad

    [Header("Instanciación Automática (si faltan en la escena)")]
    public float escalaRueda = 0.075f;
    public Vector2 posRuedaDelantera = new Vector2(1.32f, -0.62f);
    public Vector2 posRuedaTrasera = new Vector2(-1.32f, -0.62f);

    [Header("Efecto Suspensión / Rebote")]
    public float frecuenciaRebote = 2.0f;      // Frecuencia del vaivén principal (se escala con la velocidad)
    public float amplitudRebote = 0.015f;     // Amplitud del vaivén principal (sutil)
    public float frecuenciaVibracion = 25f;   // Frecuencia del traqueteo del motor
    public float amplitudVibracion = 0.003f;  // Amplitud de la vibración del motor (mínima)

    private SpriteRenderer[] ruedasEnHijos;
    private Transform visualChassis;
    private Vector3 posicionOriginalChassis;
    private float bounceTimer = 0f;
    private int chassisSortingOrder = 10;

    // Posición original de la camioneta en la escena (antes de aplicar posicionGuardada).
    // Se usa para calcular el offset correcto de las ruedas al parentearlas.
    private Vector3 posicionCamionetaEnEscena;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Guardar la posición original de la camioneta en la escena ANTES de moverla.
        // Las ruedas también están en sus posiciones de la escena, así que el offset es:
        //   ruedaOffset = ruedaPosWorld - posicionCamionetaEnEscena
        // Ese offset se mantiene al parentear.
        posicionCamionetaEnEscena = transform.position;

        if (posicionGuardada.HasValue)
        {
            transform.position = posicionGuardada.Value;
            posicionGuardada = null; // Clear immediately after applying
        }

        CrearVisualChassis();
        VincularYBuscarRuedas();
    }

    private void CrearVisualChassis()
    {
        SpriteRenderer originalSR = GetComponent<SpriteRenderer>();
        if (originalSR == null) return;

        // Forzar a 1 para asegurar que los NPCs/objetos (2 o 3) se rendericen por delante y el fondo (0 o 1) por detrás
        chassisSortingOrder = 1;

        // Crear el objeto hijo para renderizar el sprite de la camioneta
        GameObject visualGO = new GameObject("Chassis_Visual");
        visualGO.transform.SetParent(transform, false);
        visualGO.transform.localPosition = Vector3.zero;
        visualGO.transform.localRotation = Quaternion.identity;
        visualGO.transform.localScale = Vector3.one;

        // Copiar las propiedades de dibujo
        SpriteRenderer nuevoSR = visualGO.AddComponent<SpriteRenderer>();
        nuevoSR.sprite = originalSR.sprite;
        nuevoSR.color = originalSR.color;
        nuevoSR.material = originalSR.material;
        nuevoSR.sortingLayerID = originalSR.sortingLayerID;
        nuevoSR.sortingLayerName = originalSR.sortingLayerName;
        
        // El chasis y las ruedas usan chassisSortingOrder para estar por encima del fondo pero por detrás de NPCs/objetos
        nuevoSR.sortingOrder = chassisSortingOrder;
        nuevoSR.flipX = originalSR.flipX;
        nuevoSR.flipY = originalSR.flipY;

        // Desactivar el SpriteRenderer en la raíz física para que no esté duplicado
        originalSR.enabled = false;

        visualChassis = visualGO.transform;
        posicionOriginalChassis = visualChassis.localPosition;
    }

    private void VincularYBuscarRuedas()
    {
        if (spriteRueda == null)
        {
            spriteRueda = Resources.Load<Sprite>("Sprites/rueda");
        }

        System.Collections.Generic.List<SpriteRenderer> listaRuedas = new System.Collections.Generic.List<SpriteRenderer>();

        // --- Estrategia 1: buscar por nombre directamente (más robusto, incluye inactivos) ---
        string[] nombresRueda = { "rueda", "rueda (1)", "rueda (2)", "rueda (3)", "wheel", "wheel (1)" };
        foreach (string nombre in nombresRueda)
        {
            GameObject rGO = GameObject.Find(nombre);
            if (rGO == null)
                rGO = BuscarPorNombreIncluyendoInactivos(nombre);

            if (rGO != null && rGO != gameObject && (visualChassis == null || rGO != visualChassis.gameObject))
            {
                SpriteRenderer sr = rGO.GetComponent<SpriteRenderer>();
                if (sr != null && !listaRuedas.Contains(sr))
                    listaRuedas.Add(sr);
            }
        }

        // --- Estrategia 2: FindObjectsOfType incluyendo inactivos ---
        SpriteRenderer[] todosLosSR = FindObjectsOfType<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in todosLosSR)
        {
            if (sr.gameObject == gameObject) continue;
            if (visualChassis != null && sr.gameObject == visualChassis.gameObject) continue;
            if (listaRuedas.Contains(sr)) continue;

            bool esRueda = false;
            if (spriteRueda != null && sr.sprite == spriteRueda)
                esRueda = true;
            else if (sr.gameObject.name.ToLower().Contains("rueda") || sr.gameObject.name.ToLower().Contains("wheel"))
                esRueda = true;

            if (esRueda)
                listaRuedas.Add(sr);
        }

        // Si no se encontraron ruedas en la escena, las instanciamos automáticamente usando las constantes del nivel 1
        if (listaRuedas.Count == 0 && spriteRueda != null)
        {
            // Valores calibrados exactamente del nivel 1 (YPF) relativos al Rastrojero
            float targetFrontOffsetX = 0.973f;
            float targetFrontOffsetY = -0.555f;
            float targetRearOffsetX = -1.236f;
            float targetRearOffsetY = -0.595f;
            float targetWorldScale = 0.065688f;

            float localFrontOffsetX = targetFrontOffsetX / transform.lossyScale.x;
            float localFrontOffsetY = targetFrontOffsetY / transform.lossyScale.y;
            float localRearOffsetX = targetRearOffsetX / transform.lossyScale.x;
            float localRearOffsetY = targetRearOffsetY / transform.lossyScale.y;
            float localScaleVal = targetWorldScale / transform.lossyScale.x;

            // Instanciar rueda delantera
            GameObject rd = new GameObject("rueda_delantera_auto");
            SpriteRenderer srD = rd.AddComponent<SpriteRenderer>();
            srD.sprite = spriteRueda;
            rd.transform.SetParent(transform, false);
            rd.transform.localPosition = new Vector3(localFrontOffsetX, localFrontOffsetY, -0.05f);
            rd.transform.localScale = new Vector3(localScaleVal, localScaleVal, 1f);
            srD.sortingOrder = chassisSortingOrder;
            listaRuedas.Add(srD);

            // Instanciar rueda trasera
            GameObject rt = new GameObject("rueda_trasera_auto");
            SpriteRenderer srT = rt.AddComponent<SpriteRenderer>();
            srT.sprite = spriteRueda;
            rt.transform.SetParent(transform, false);
            rt.transform.localPosition = new Vector3(localRearOffsetX, localRearOffsetY, -0.05f);
            rt.transform.localScale = new Vector3(localScaleVal, localScaleVal, 1f);
            srT.sortingOrder = chassisSortingOrder;
            listaRuedas.Add(srT);

            Debug.Log("[Movimiento] Ruedas creadas dinámicamente con offsets de YPF.");
        }
        else
        {
            // Parentear y configurar cada rueda existente en la escena
            foreach (SpriteRenderer sr in listaRuedas)
            {
                if (!sr.gameObject.activeSelf)
                    sr.gameObject.SetActive(true);

                // Calcular el offset correcto de la rueda respecto a la camioneta.
                // Las ruedas están en sus posiciones originales de la escena.
                // La camioneta puede haber sido teletransportada a posicionGuardada,
                // así que debemos calcular el offset usando la posición original de la escena.
                Vector3 offsetMundial = sr.transform.position - posicionCamionetaEnEscena;

                // Guardar la escala mundial ANTES de parentear (para restaurarla después)
                Vector3 escalaWorldOriginal = sr.transform.lossyScale;

                // Parentear SIN mantener posición mundial (ya la calculamos manualmente)
                sr.transform.SetParent(transform, false);

                // Asignar el offset correcto como posición local
                sr.transform.localPosition = new Vector3(
                    offsetMundial.x / transform.lossyScale.x,
                    offsetMundial.y / transform.lossyScale.y,
                    -0.05f
                );

                // Restaurar la escala mundial original dividiendo por la escala del padre.
                // Así el tamaño visual de la rueda no cambia al volverse hija de la camioneta.
                Vector3 parentScale = transform.lossyScale;
                sr.transform.localScale = new Vector3(
                    escalaWorldOriginal.x / parentScale.x,
                    escalaWorldOriginal.y / parentScale.y,
                    escalaWorldOriginal.z / parentScale.z
                );

                // Reajustar sorting order
                sr.sortingOrder = chassisSortingOrder;
            }
        }

        ruedasEnHijos = listaRuedas.ToArray();

        if (ruedasEnHijos.Length == 0)
            Debug.LogWarning("[Movimiento] No se encontraron ruedas en la escena.");
        else
            Debug.Log($"[Movimiento] Ruedas vinculadas: {ruedasEnHijos.Length}");
    }

    /// <summary>
    /// Busca un GameObject por nombre incluyendo objetos inactivos en toda la jerarquía.
    /// </summary>
    private GameObject BuscarPorNombreIncluyendoInactivos(string nombre)
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.hideFlags != HideFlags.None) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            if (t.gameObject.name == nombre)
                return t.gameObject;
        }
        return null;
    }

    public static void GuardarPosicion(Vector3 pos)
    {
        posicionGuardada = pos;
    }

    public static void ResetearPosicion()
    {
        posicionGuardada = null;
    }

    void FixedUpdate()
    {
        float h = 0f;

        // Sin combustible: frenar gradualmente
        if (InventoryManager.CurrentFuel <= 0f)
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, (velocidad / tiempoFrenado) * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);
            RotarRuedas();
            AnimarSuspension();
            return;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;

        if (h > 0)
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidad, aceleracion * Time.fixedDeltaTime);
        else if (h < 0)
            velocidadActual = Mathf.MoveTowards(velocidadActual, -velocidad, aceleracion * Time.fixedDeltaTime);
        else
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, (velocidad / tiempoFrenado) * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(velocidadActual, rb.linearVelocity.y);

        RotarRuedas();
        AnimarSuspension();
    }

    private void RotarRuedas()
    {
        if (rb != null && ruedasEnHijos != null)
        {
            float speed = rb.linearVelocity.x;
            float rotacionZ = -speed * multiplicadorGiro * Time.fixedDeltaTime;
            
            foreach (SpriteRenderer cmd in ruedasEnHijos)
            {
                if (cmd != null)
                {
                    cmd.transform.Rotate(0f, 0f, rotacionZ);
                }
            }
        }
    }

    private void AnimarSuspension()
    {
        if (visualChassis == null || rb == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);

        if (speed > 0.05f)
        {
            // Avanzar el temporizador del vaivén según la velocidad
            bounceTimer += Time.fixedDeltaTime * speed * frecuenciaRebote;

            // 1. Vaivén suave de la suspensión
            float bounceY = Mathf.Sin(bounceTimer) * amplitudRebote;

            // 2. Traqueteo constante del motor
            float vibracionY = Mathf.Sin(Time.time * frecuenciaVibracion) * amplitudVibracion;

            // 3. Cabceo ligero al moverse (inclinación sutil)
            float pitchAngle = -rb.linearVelocity.x * 0.04f; 
            visualChassis.localRotation = Quaternion.Euler(0f, 0f, pitchAngle);

            // Aplicar traslación local al chasis
            visualChassis.localPosition = new Vector3(
                posicionOriginalChassis.x,
                posicionOriginalChassis.y + bounceY + vibracionY,
                posicionOriginalChassis.z
            );
        }
        else
        {
            // En reposo, estabilizar suavemente el chasis a su posición y rotación original
            visualChassis.localPosition = Vector3.MoveTowards(
                visualChassis.localPosition,
                posicionOriginalChassis,
                Time.fixedDeltaTime * 0.2f
            );
            visualChassis.localRotation = Quaternion.RotateTowards(
                visualChassis.localRotation,
                Quaternion.identity,
                Time.fixedDeltaTime * 10f
            );
            bounceTimer = 0f;
        }
    }
}