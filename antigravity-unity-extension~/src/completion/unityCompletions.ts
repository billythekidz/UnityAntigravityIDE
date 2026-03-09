import * as vscode from 'vscode';

/**
 * Unity API message methods with documentation.
 * Used for IntelliSense completions in C# files within Unity projects.
 */
interface UnityMessage {
    name: string;
    signature: string;
    description: string;
    category: string;
}

const UNITY_MESSAGES: UnityMessage[] = [
    // Initialization
    { name: 'Awake', signature: 'void Awake()', description: 'Called when the script instance is being loaded. Awake is called before Start.', category: 'Initialization' },
    { name: 'OnEnable', signature: 'void OnEnable()', description: 'Called when the object becomes enabled and active.', category: 'Initialization' },
    { name: 'Start', signature: 'void Start()', description: 'Called before the first frame update, after Awake.', category: 'Initialization' },

    // Update
    { name: 'Update', signature: 'void Update()', description: 'Called every frame. Use for regular updates.', category: 'Update' },
    { name: 'LateUpdate', signature: 'void LateUpdate()', description: 'Called every frame after all Update functions. Useful for camera follow.', category: 'Update' },
    { name: 'FixedUpdate', signature: 'void FixedUpdate()', description: 'Called at fixed intervals. Use for physics updates.', category: 'Update' },

    // Lifecycle
    { name: 'OnDisable', signature: 'void OnDisable()', description: 'Called when the behaviour becomes disabled.', category: 'Lifecycle' },
    { name: 'OnDestroy', signature: 'void OnDestroy()', description: 'Called when the MonoBehaviour will be destroyed.', category: 'Lifecycle' },
    { name: 'OnApplicationQuit', signature: 'void OnApplicationQuit()', description: 'Called before the application is quit.', category: 'Lifecycle' },
    { name: 'OnApplicationFocus', signature: 'void OnApplicationFocus(bool hasFocus)', description: 'Called when the player gets or loses focus.', category: 'Lifecycle' },
    { name: 'OnApplicationPause', signature: 'void OnApplicationPause(bool pauseStatus)', description: 'Called when the player pauses.', category: 'Lifecycle' },

    // Physics 3D
    { name: 'OnCollisionEnter', signature: 'void OnCollisionEnter(Collision collision)', description: 'Called when this collider/rigidbody has begun touching another rigidbody/collider.', category: 'Physics 3D' },
    { name: 'OnCollisionStay', signature: 'void OnCollisionStay(Collision collision)', description: 'Called once per frame while touching another rigidbody/collider.', category: 'Physics 3D' },
    { name: 'OnCollisionExit', signature: 'void OnCollisionExit(Collision collision)', description: 'Called when this collider/rigidbody has stopped touching another rigidbody/collider.', category: 'Physics 3D' },
    { name: 'OnTriggerEnter', signature: 'void OnTriggerEnter(Collider other)', description: 'Called when a Collider enters the trigger.', category: 'Physics 3D' },
    { name: 'OnTriggerStay', signature: 'void OnTriggerStay(Collider other)', description: 'Called once per frame while a Collider stays in the trigger.', category: 'Physics 3D' },
    { name: 'OnTriggerExit', signature: 'void OnTriggerExit(Collider other)', description: 'Called when a Collider exits the trigger.', category: 'Physics 3D' },
    { name: 'OnControllerColliderHit', signature: 'void OnControllerColliderHit(ControllerColliderHit hit)', description: 'Called when the CharacterController hits a collider while performing a Move.', category: 'Physics 3D' },
    { name: 'OnJointBreak', signature: 'void OnJointBreak(float breakForce)', description: 'Called when a joint attached to the same game object broke.', category: 'Physics 3D' },

    // Physics 2D
    { name: 'OnCollisionEnter2D', signature: 'void OnCollisionEnter2D(Collision2D collision)', description: 'Called when an incoming collider makes contact with this 2D collider.', category: 'Physics 2D' },
    { name: 'OnCollisionStay2D', signature: 'void OnCollisionStay2D(Collision2D collision)', description: 'Called each frame where a collider on another 2D object is touching this 2D collider.', category: 'Physics 2D' },
    { name: 'OnCollisionExit2D', signature: 'void OnCollisionExit2D(Collision2D collision)', description: 'Called when a collider on another 2D object stops touching this 2D collider.', category: 'Physics 2D' },
    { name: 'OnTriggerEnter2D', signature: 'void OnTriggerEnter2D(Collider2D other)', description: 'Called when another 2D object enters the 2D trigger collider.', category: 'Physics 2D' },
    { name: 'OnTriggerStay2D', signature: 'void OnTriggerStay2D(Collider2D other)', description: 'Called each frame where another 2D object is within a 2D trigger collider.', category: 'Physics 2D' },
    { name: 'OnTriggerExit2D', signature: 'void OnTriggerExit2D(Collider2D other)', description: 'Called when another 2D object leaves the 2D trigger collider.', category: 'Physics 2D' },
    { name: 'OnJointBreak2D', signature: 'void OnJointBreak2D(Joint2D brokenJoint)', description: 'Called when a Joint2D attached to the same game object breaks.', category: 'Physics 2D' },

    // Mouse
    { name: 'OnMouseDown', signature: 'void OnMouseDown()', description: 'Called when the user presses the mouse button over the Collider.', category: 'Mouse' },
    { name: 'OnMouseUp', signature: 'void OnMouseUp()', description: 'Called when the user releases the mouse button.', category: 'Mouse' },
    { name: 'OnMouseEnter', signature: 'void OnMouseEnter()', description: 'Called when the mouse enters the Collider.', category: 'Mouse' },
    { name: 'OnMouseExit', signature: 'void OnMouseExit()', description: 'Called when the mouse exits the Collider.', category: 'Mouse' },
    { name: 'OnMouseOver', signature: 'void OnMouseOver()', description: 'Called every frame while the mouse is over the Collider.', category: 'Mouse' },
    { name: 'OnMouseDrag', signature: 'void OnMouseDrag()', description: 'Called when the user clicks on a Collider and holds the mouse button.', category: 'Mouse' },
    { name: 'OnMouseUpAsButton', signature: 'void OnMouseUpAsButton()', description: 'Called when the mouse button is released over the same Collider it was pressed on.', category: 'Mouse' },

    // Rendering
    { name: 'OnBecameVisible', signature: 'void OnBecameVisible()', description: 'Called when the renderer became visible by any camera.', category: 'Rendering' },
    { name: 'OnBecameInvisible', signature: 'void OnBecameInvisible()', description: 'Called when the renderer is no longer visible by any camera.', category: 'Rendering' },
    { name: 'OnRenderObject', signature: 'void OnRenderObject()', description: 'Called after camera has rendered the scene.', category: 'Rendering' },
    { name: 'OnPreRender', signature: 'void OnPreRender()', description: 'Called before the camera starts rendering the scene.', category: 'Rendering' },
    { name: 'OnPostRender', signature: 'void OnPostRender()', description: 'Called after the camera finishes rendering the scene.', category: 'Rendering' },
    { name: 'OnRenderImage', signature: 'void OnRenderImage(RenderTexture src, RenderTexture dest)', description: 'Called after all rendering to allow post-processing.', category: 'Rendering' },
    { name: 'OnPreCull', signature: 'void OnPreCull()', description: 'Called before the camera culls the scene.', category: 'Rendering' },

    // Gizmos
    { name: 'OnDrawGizmos', signature: 'void OnDrawGizmos()', description: 'Implement to draw gizmos that are always drawn.', category: 'Gizmos' },
    { name: 'OnDrawGizmosSelected', signature: 'void OnDrawGizmosSelected()', description: 'Implement to draw gizmos only when the object is selected.', category: 'Gizmos' },

    // Editor
    { name: 'OnGUI', signature: 'void OnGUI()', description: 'Called for rendering and handling GUI events.', category: 'GUI' },
    { name: 'OnValidate', signature: 'void OnValidate()', description: 'Called in the editor when the script is loaded or a value is changed in the Inspector.', category: 'Editor' },
    { name: 'Reset', signature: 'void Reset()', description: 'Called when the user hits the Reset button in the Inspector or when first added.', category: 'Editor' },

    // Animation
    { name: 'OnAnimatorIK', signature: 'void OnAnimatorIK(int layerIndex)', description: 'Callback for setting up animation IK.', category: 'Animation' },
    { name: 'OnAnimatorMove', signature: 'void OnAnimatorMove()', description: 'Callback for processing animation movements for modifying root motion.', category: 'Animation' },

    // Audio
    { name: 'OnAudioFilterRead', signature: 'void OnAudioFilterRead(float[] data, int channels)', description: 'Called every time an AudioClip is read by the AudioSource to allow DSP.', category: 'Audio' },

    // Particles
    { name: 'OnParticleCollision', signature: 'void OnParticleCollision(GameObject other)', description: 'Called when a particle hits a Collider.', category: 'Particles' },
    { name: 'OnParticleSystemStopped', signature: 'void OnParticleSystemStopped()', description: 'Called when all particles in the system have died.', category: 'Particles' },
    { name: 'OnParticleTrigger', signature: 'void OnParticleTrigger()', description: 'Called when particles meet the conditions of the trigger module.', category: 'Particles' },

    // Transform
    { name: 'OnTransformParentChanged', signature: 'void OnTransformParentChanged()', description: 'Called when the parent property of the transform has changed.', category: 'Transform' },
    { name: 'OnTransformChildrenChanged', signature: 'void OnTransformChildrenChanged()', description: 'Called when the list of children of the transform has changed.', category: 'Transform' },
];

export function registerCompletionProviders(context: vscode.ExtensionContext) {
    // C# Unity message completion
    const csharpProvider = vscode.languages.registerCompletionItemProvider(
        { language: 'csharp', scheme: 'file' },
        {
            provideCompletionItems(
                document: vscode.TextDocument,
                position: vscode.Position,
                token: vscode.CancellationToken,
                completionContext: vscode.CompletionContext
            ): vscode.CompletionItem[] {
                // Only provide completions inside a class body
                const lineText = document.lineAt(position.line).text;
                const linePrefix = lineText.substring(0, position.character);

                // Check if we're likely inside a class body (simple heuristic)
                if (!isInsideClassBody(document, position)) {
                    return [];
                }

                return UNITY_MESSAGES.map(msg => {
                    const item = new vscode.CompletionItem(
                        msg.name,
                        vscode.CompletionItemKind.Method
                    );

                    item.detail = `${msg.signature} [${msg.category}]`;
                    item.documentation = new vscode.MarkdownString(
                        `**Unity API Message** — ${msg.category}\n\n${msg.description}\n\n\`\`\`csharp\nprivate ${msg.signature}\n{\n    \n}\n\`\`\``
                    );

                    // Insert the full method
                    const params = extractParams(msg.signature);
                    item.insertText = new vscode.SnippetString(
                        `private ${msg.signature}\n{\n    $0\n}`
                    );

                    item.sortText = `0_unity_${msg.name}`;
                    item.filterText = msg.name;

                    return item;
                });
            }
        }
    );

    // Unity API hover provider
    const hoverProvider = vscode.languages.registerHoverProvider(
        { language: 'csharp', scheme: 'file' },
        {
            provideHover(
                document: vscode.TextDocument,
                position: vscode.Position,
                token: vscode.CancellationToken
            ): vscode.Hover | undefined {
                const wordRange = document.getWordRangeAtPosition(position);
                if (!wordRange) return undefined;

                const word = document.getText(wordRange);
                const msg = UNITY_MESSAGES.find(m => m.name === word);

                if (msg) {
                    const md = new vscode.MarkdownString();
                    md.appendMarkdown(`### Unity API: ${msg.name}\n\n`);
                    md.appendMarkdown(`**Category:** ${msg.category}\n\n`);
                    md.appendMarkdown(`${msg.description}\n\n`);
                    md.appendCodeblock(`private ${msg.signature}\n{\n    \n}`, 'csharp');
                    md.appendMarkdown(`\n\n[📖 Unity Docs](https://docs.unity3d.com/ScriptReference/MonoBehaviour.${msg.name}.html)`);
                    return new vscode.Hover(md, wordRange);
                }

                return undefined;
            }
        }
    );

    context.subscriptions.push(csharpProvider, hoverProvider);
}

function isInsideClassBody(document: vscode.TextDocument, position: vscode.Position): boolean {
    let braceCount = 0;
    let foundClass = false;

    for (let i = 0; i <= position.line; i++) {
        const line = document.lineAt(i).text;

        if (line.match(/\bclass\b/) && line.match(/:\s*(MonoBehaviour|NetworkBehaviour|ScriptableObject)/)) {
            foundClass = true;
        }

        for (const char of line) {
            if (char === '{') braceCount++;
            if (char === '}') braceCount--;
        }
    }

    return foundClass && braceCount > 0;
}

function extractParams(signature: string): string {
    const match = signature.match(/\((.*)\)/);
    return match ? match[1] : '';
}
