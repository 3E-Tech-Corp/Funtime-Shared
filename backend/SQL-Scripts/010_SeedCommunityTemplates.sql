-- ============================================================================
-- Seed Initial Templates for pickleball.community
-- ============================================================================

USE FuntimeIdentity;
GO

-- Welcome Email
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'welcome' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'welcome',
        'email',
        'en',
        'Welcome to Pickleball Community!',
        '<h1>Welcome, {{firstName}}!</h1>
<p>Thanks for joining Pickleball Community. We''re excited to have you!</p>
<h2>What''s Next?</h2>
<ul>
  <li><strong>Complete your profile</strong> - Add your skill level and preferences</li>
  <li><strong>Find events</strong> - Browse tournaments and open play near you</li>
  <li><strong>Connect</strong> - Meet other players in your area</li>
</ul>
<p>If you have any questions, just reply to this email.</p>
<p>See you on the courts!<br>The Pickleball Community Team</p>',
        'Welcome, {{firstName}}!

Thanks for joining Pickleball Community. We''re excited to have you!

What''s Next?
- Complete your profile - Add your skill level and preferences
- Find events - Browse tournaments and open play near you
- Connect - Meet other players in your area

See you on the courts!
The Pickleball Community Team'
    );
    PRINT 'Inserted welcome template';
END
GO

-- Tournament Reminder
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'tournament_reminder' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'tournament_reminder',
        'email',
        'en',
        'Reminder: {{eventName}} is coming up!',
        '<h1>Don''t Forget!</h1>
<p>Hi {{firstName}},</p>
<p>Just a friendly reminder that <strong>{{eventName}}</strong> is coming up:</p>
<ul>
  <li><strong>Date:</strong> {{eventDate}}</li>
  <li><strong>Location:</strong> {{venue}}</li>
  <li><strong>Check-in:</strong> {{checkInTime}}</li>
</ul>
<p><strong>What to bring:</strong></p>
<ul>
  <li>Your paddle and balls</li>
  <li>Water and snacks</li>
  <li>Comfortable court shoes</li>
</ul>
<p><a href="{{eventUrl}}">View Event Details</a></p>
<p>Good luck and have fun!</p>',
        'Don''t Forget!

Hi {{firstName}},

Just a reminder that {{eventName}} is coming up:

Date: {{eventDate}}
Location: {{venue}}
Check-in: {{checkInTime}}

What to bring:
- Your paddle and balls
- Water and snacks
- Comfortable court shoes

Good luck and have fun!'
    );
    PRINT 'Inserted tournament_reminder template';
END
GO

-- Registration Confirmation
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'registration_confirmation' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'registration_confirmation',
        'email',
        'en',
        'Registration Confirmed: {{eventName}}',
        '<h1>You''re Registered!</h1>
<p>Hi {{firstName}},</p>
<p>Your registration for <strong>{{eventName}}</strong> has been confirmed.</p>
<h2>Registration Details</h2>
<ul>
  <li><strong>Event:</strong> {{eventName}}</li>
  <li><strong>Division:</strong> {{divisionName}}</li>
  <li><strong>Date:</strong> {{eventDate}}</li>
  <li><strong>Location:</strong> {{venue}}</li>
  {{#if partnerName}}<li><strong>Partner:</strong> {{partnerName}}</li>{{/if}}
</ul>
{{#if amountPaid}}
<h2>Payment</h2>
<p>Amount paid: <strong>${{amountPaid}}</strong></p>
<p>Confirmation #: {{confirmationNumber}}</p>
{{/if}}
<p><a href="{{eventUrl}}">View Event Details</a></p>
<p>See you there!</p>',
        'You''re Registered!

Hi {{firstName}},

Your registration for {{eventName}} has been confirmed.

Event: {{eventName}}
Division: {{divisionName}}
Date: {{eventDate}}
Location: {{venue}}
{{#if partnerName}}Partner: {{partnerName}}{{/if}}

{{#if amountPaid}}
Amount paid: ${{amountPaid}}
Confirmation #: {{confirmationNumber}}
{{/if}}

See you there!'
    );
    PRINT 'Inserted registration_confirmation template';
END
GO

-- Payment Receipt
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'payment_receipt' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'payment_receipt',
        'email',
        'en',
        'Payment Receipt - Pickleball Community',
        '<h1>Payment Receipt</h1>
<p>Hi {{firstName}},</p>
<p>Thank you for your payment. Here are your receipt details:</p>
<table style="border-collapse: collapse; width: 100%; max-width: 500px;">
  <tr>
    <td style="padding: 8px; border-bottom: 1px solid #ddd;"><strong>Date:</strong></td>
    <td style="padding: 8px; border-bottom: 1px solid #ddd;">{{paymentDate}}</td>
  </tr>
  <tr>
    <td style="padding: 8px; border-bottom: 1px solid #ddd;"><strong>Description:</strong></td>
    <td style="padding: 8px; border-bottom: 1px solid #ddd;">{{description}}</td>
  </tr>
  <tr>
    <td style="padding: 8px; border-bottom: 1px solid #ddd;"><strong>Amount:</strong></td>
    <td style="padding: 8px; border-bottom: 1px solid #ddd;">${{amount}}</td>
  </tr>
  <tr>
    <td style="padding: 8px;"><strong>Confirmation #:</strong></td>
    <td style="padding: 8px;">{{confirmationNumber}}</td>
  </tr>
</table>
<p style="margin-top: 20px; color: #666;">This receipt was sent to {{email}}. Please save it for your records.</p>',
        'Payment Receipt

Hi {{firstName}},

Thank you for your payment.

Date: {{paymentDate}}
Description: {{description}}
Amount: ${{amount}}
Confirmation #: {{confirmationNumber}}

This receipt was sent to {{email}}. Please save it for your records.'
    );
    PRINT 'Inserted payment_receipt template';
END
GO

-- Schedule Update
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'schedule_update' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'schedule_update',
        'email',
        'en',
        'Schedule Update: {{eventName}}',
        '<h1>Schedule Update</h1>
<p>Hi {{firstName}},</p>
<p>There has been a schedule update for <strong>{{eventName}}</strong>:</p>
<div style="background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 15px 0;">
  <p><strong>{{updateType}}</strong></p>
  <p>{{updateDetails}}</p>
</div>
{{#if newTime}}
<p><strong>New Time:</strong> {{newTime}}</p>
{{/if}}
{{#if newCourt}}
<p><strong>New Court:</strong> {{newCourt}}</p>
{{/if}}
<p><a href="{{eventUrl}}">View Updated Schedule</a></p>
<p>Please arrive on time. Thank you for your understanding!</p>',
        'Schedule Update

Hi {{firstName}},

There has been a schedule update for {{eventName}}:

{{updateType}}
{{updateDetails}}

{{#if newTime}}New Time: {{newTime}}{{/if}}
{{#if newCourt}}New Court: {{newCourt}}{{/if}}

Please arrive on time. Thank you for your understanding!'
    );
    PRINT 'Inserted schedule_update template';
END
GO

-- Match Result
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'match_result' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'match_result',
        'email',
        'en',
        'Match Result: {{eventName}}',
        '<h1>Match Complete</h1>
<p>Hi {{firstName}},</p>
<p>Your match at <strong>{{eventName}}</strong> has been recorded:</p>
<div style="background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 15px 0; text-align: center;">
  <p style="font-size: 24px; margin: 0;"><strong>{{score}}</strong></p>
  <p style="color: #666; margin: 5px 0;">{{team1}} vs {{team2}}</p>
</div>
<p><strong>Division:</strong> {{divisionName}}</p>
<p><strong>Round:</strong> {{roundName}}</p>
{{#if nextMatch}}
<p><strong>Next Match:</strong> {{nextMatch}}</p>
{{/if}}
<p><a href="{{bracketUrl}}">View Bracket</a></p>',
        'Match Complete

Hi {{firstName}},

Your match at {{eventName}} has been recorded:

Score: {{score}}
{{team1}} vs {{team2}}

Division: {{divisionName}}
Round: {{roundName}}
{{#if nextMatch}}Next Match: {{nextMatch}}{{/if}}'
    );
    PRINT 'Inserted match_result template';
END
GO

-- Password Reset
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'password_reset' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'password_reset',
        'email',
        'en',
        'Reset Your Password - Pickleball Community',
        '<h1>Password Reset Request</h1>
<p>Hi {{firstName}},</p>
<p>We received a request to reset your password. Click the button below to create a new password:</p>
<p style="text-align: center; margin: 30px 0;">
  <a href="{{resetUrl}}" style="background: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">Reset Password</a>
</p>
<p>This link will expire in {{expiresIn}}.</p>
<p style="color: #666;">If you didn''t request this, you can safely ignore this email. Your password won''t change.</p>',
        'Password Reset Request

Hi {{firstName}},

We received a request to reset your password. Visit this link to create a new password:

{{resetUrl}}

This link will expire in {{expiresIn}}.

If you didn''t request this, you can safely ignore this email.'
    );
    PRINT 'Inserted password_reset template';
END
GO

-- Email Verification
IF NOT EXISTS (SELECT 1 FROM NotificationTemplates WHERE SiteKey = 'community' AND TypeCode = 'email_verification' AND ChannelCode = 'email')
BEGIN
    INSERT INTO NotificationTemplates (SiteKey, TypeCode, ChannelCode, LangCode, Subject, BodyHtml, BodyText)
    VALUES (
        'community',
        'email_verification',
        'email',
        'en',
        'Verify Your Email - Pickleball Community',
        '<h1>Verify Your Email</h1>
<p>Hi {{firstName}},</p>
<p>Please verify your email address by clicking the button below:</p>
<p style="text-align: center; margin: 30px 0;">
  <a href="{{verifyUrl}}" style="background: #2196F3; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">Verify Email</a>
</p>
<p>This link will expire in {{expiresIn}}.</p>
<p style="color: #666;">If you didn''t create an account, you can ignore this email.</p>',
        'Verify Your Email

Hi {{firstName}},

Please verify your email address by visiting this link:

{{verifyUrl}}

This link will expire in {{expiresIn}}.

If you didn''t create an account, you can ignore this email.'
    );
    PRINT 'Inserted email_verification template';
END
GO

PRINT '';
PRINT '=== Templates seeded successfully for pickleball.community ===';
GO
