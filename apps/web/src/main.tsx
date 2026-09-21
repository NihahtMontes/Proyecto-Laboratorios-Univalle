import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { App } from './App';
import '../../../wwwroot/assets/css/style.min.css';
import '../../../wwwroot/dist/css/icons/material-design-iconic-font/css/materialdesignicons.min.css';
import '../../../wwwroot/dist/css/icons/font-awesome/css/fontawesome-all.min.css';
import '../../../wwwroot/dist/css/icons/themify-icons/themify-icons.css';
import './styles.css';

const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('No se encontró el contenedor #root');
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
