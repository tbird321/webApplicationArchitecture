import React from 'react';
import AppLayout from './AppLayout';

function Contact() {
  return (
    <AppLayout>
    <div>
      <h1>Contact Page</h1>
      <p>You can reach us at:</p>
      <ul>
        <li>Email: contact@example.com</li>
        <li>Phone: +1 (123) 456-7890</li>
        <li>Address: 123 Main St, Cityville</li>
      </ul>
    </div>
    </AppLayout>
  );
}

export default Contact;
