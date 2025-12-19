window.submitFallbackLogin = (user, password, returnUrl, sessionId) => {
    try {
        console.log('submitFallbackLogin called', { user, returnUrl, sessionId });
        // Use fetch POST so we can wait for server to set cookie before navigating
        const fd = new FormData();
        fd.append('user', user || '');
        fd.append('password', password || '');
        fd.append('returnUrl', returnUrl || '/');
        fd.append('sessionId', sessionId || '');

        fetch('/auth/login', {
            method: 'POST',
            body: fd,
            credentials: 'same-origin'
        }).then(async (resp) => {
            console.log('fallback login response', resp.status, resp.redirected, resp.url);
            // navigate to returnUrl after server has processed sign-in
            try {
                const dest = returnUrl || '/';
                window.location.href = dest;
            } catch (e) { console.error(e); }
        }).catch(e => {
            console.error('fallback login fetch failed', e);
            // last resort: submit form
            const form = document.getElementById('fallbackLoginForm');
            if (form) {
                document.getElementById('fallback-user').value = user || '';
                document.getElementById('fallback-password').value = password || '';
                document.getElementById('fallback-returnUrl').value = returnUrl || '/';
                document.getElementById('fallback-sessionId').value = sessionId || '';
                form.submit();
            }
        });
    } catch (e) {
        console.error('fallback login submit failed', e);
    }
};
