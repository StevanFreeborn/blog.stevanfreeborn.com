export function onLoad() {
  init();
}

export function onUpdate() {
  init();
}

function init(){
  addAnchors();
  addClipboard();
  Prism.highlightAll();
}

/**
 * @summary Adds anchors to each heading in the post
 * so that users can link directly to them. 
*/ 
function addAnchors() {
  const selectors = [
    '.markdown-body h2',
    '.markdown-body h3',
    '.markdown-body h4',
    '.markdown-body h5',
    '.markdown-body h6'
  ];

  // relies on AnchorJS library
  // being loaded in Post.razor
  anchors.add(selectors.join(','));
}

/**
 * @summary Adds a button to each code block that
 * allows users to copy the code to their clipboard.
 */
function addClipboard() {
  const codeBlocks = document.querySelectorAll('pre');
  
  codeBlocks.forEach((block) => {
    if (block.querySelector('button.copy-button')) {
      return;
    }

    const button = document.createElement('button');
    button.className = 'copy-button';
    button.type = 'button';
    button.innerHTML = `
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 448 512" fill="currentColor">
        <path d="M384 336H192c-8.8 0-16-7.2-16-16V64c0-8.8 7.2-16 16-16l140.1 0L400 115.9V320c0 8.8-7.2 16-16 16zM192 384H384c35.3 0 64-28.7 64-64V115.9c0-12.7-5.1-24.9-14.1-33.9L366.1 14.1c-9-9-21.2-14.1-33.9-14.1H192c-35.3 0-64 28.7-64 64V320c0 35.3 28.7 64 64 64zM64 128c-35.3 0-64 28.7-64 64V448c0 35.3 28.7 64 64 64H256c35.3 0 64-28.7 64-64V416H272v32c0 8.8-7.2 16-16 16H64c-8.8 0-16-7.2-16-16V192c0-8.8 7.2-16 16-16H96V128H64z"/>
      </svg>
    `
    button.onclick = () => {
      button.style.color = 'green';
      setTimeout(() => {
        button.style.color = '';
      }, 1000);
    };

    const buttonText = document.createElement('span');
    buttonText.className = 'sr-only';
    buttonText.textContent = 'Copy';

    button.appendChild(buttonText);
    block.appendChild(button);
  });

  // relies on ClipboardJS library
  // being loaded in Post.razor
  var clipboard = new ClipboardJS('.copy-button', {
    target: (trigger) => {
      return trigger.previousElementSibling;
    }
  });

  clipboard.on('success', function(e) {
    e.clearSelection();
  });
}